#include "llama.h"
#include "mtmd.h"
#include "mtmd-helper.h"

#include <math.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#if defined(__cplusplus)
#define RECALL_EMBED_LINKAGE extern "C"
#else
#define RECALL_EMBED_LINKAGE
#endif

#if defined(_WIN32)
#define RECALL_EMBED_EXPORT RECALL_EMBED_LINKAGE __declspec(dllexport)
#else
#define RECALL_EMBED_EXPORT RECALL_EMBED_LINKAGE __attribute__((visibility("default")))
#endif

static struct llama_model * g_model = NULL;
static struct llama_context * g_ctx = NULL;
static mtmd_context * g_vision = NULL;
static mtmd_context * g_audio = NULL;
static bool g_backend_initialized = false;

#define RECALL_EMBED_DIM 768
#define RECALL_EMBED_MAX_TOKENS 7680
#define RECALL_EMBED_AUDIO_SAMPLE_RATE 16000

static int32_t recall_embed_output_width(void)
{
    const int32_t out = llama_model_n_embd_out(g_model);
    if (out > 0)
    {
        return out;
    }

    return llama_model_n_embd(g_model);
}

static int recall_embed_token_count(const char * text, int32_t * count_out)
{
    const struct llama_vocab * vocab = llama_model_get_vocab(g_model);
    const int32_t text_len = (int32_t) strlen(text);
    const int32_t n = llama_tokenize(vocab, text, text_len, NULL, 0, true, true);
    if (n == INT32_MIN)
    {
        return 3;
    }

    if (n < 0)
    {
        *count_out = -n;
        return 0;
    }

    *count_out = n;
    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_open(const char * model_path)
{
    if (model_path == NULL)
    {
        return 1;
    }

    if (g_model != NULL)
    {
        return 8;
    }

    if (!g_backend_initialized)
    {
        llama_backend_init();
        g_backend_initialized = true;
    }

    struct llama_model_params model_params = llama_model_default_params();
    model_params.n_gpu_layers = -1;
    model_params.load_mode = LLAMA_LOAD_MODE_MMAP;

    g_model = llama_model_load_from_file(model_path, model_params);
    if (g_model == NULL)
    {
        return 2;
    }

    struct llama_context_params ctx_params = llama_context_default_params();
    ctx_params.n_ctx = RECALL_EMBED_MAX_TOKENS;
    ctx_params.n_batch = RECALL_EMBED_MAX_TOKENS;
    ctx_params.n_ubatch = RECALL_EMBED_MAX_TOKENS;
    ctx_params.embeddings = true;
    ctx_params.pooling_type = LLAMA_POOLING_TYPE_MEAN;
    ctx_params.attention_type = LLAMA_ATTENTION_TYPE_NON_CAUSAL;

    g_ctx = llama_init_from_model(g_model, ctx_params);
    if (g_ctx == NULL)
    {
        llama_model_free(g_model);
        g_model = NULL;
        return 2;
    }

    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_close(void)
{
    if (g_ctx != NULL)
    {
        llama_free(g_ctx);
        g_ctx = NULL;
    }

    if (g_model != NULL)
    {
        llama_model_free(g_model);
        g_model = NULL;
    }

    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_count_tokens(const char * text, int * token_count)
{
    if (g_model == NULL)
    {
        return 9;
    }

    if (text == NULL || token_count == NULL)
    {
        return 1;
    }

    int32_t count = 0;
    const int rc = recall_embed_token_count(text, &count);
    if (rc != 0)
    {
        return rc;
    }

    *token_count = (int) count;
    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_token_ends(const char * text, int32_t * ends, int32_t ends_capacity, int32_t * ends_len)
{
    if (g_model == NULL)
    {
        return 9;
    }

    if (text == NULL || ends == NULL || ends_capacity <= 0 || ends_len == NULL)
    {
        return 1;
    }

    const struct llama_vocab * vocab = llama_model_get_vocab(g_model);
    const int32_t text_len = (int32_t) strlen(text);
    const int32_t n_neg = llama_tokenize(vocab, text, text_len, NULL, 0, false, true);
    if (n_neg == INT32_MIN || n_neg >= 0)
    {
        return 3;
    }

    const int32_t n_tokens = -n_neg;
    llama_token * tokens = (llama_token *) malloc((size_t) n_tokens * sizeof(llama_token));
    if (tokens == NULL)
    {
        return 5;
    }

    const int32_t n_filled = llama_tokenize(vocab, text, text_len, tokens, n_tokens, false, true);
    if (n_filled < 0)
    {
        free(tokens);
        return 3;
    }

    const int32_t cap = n_tokens < RECALL_EMBED_MAX_TOKENS ? n_tokens : RECALL_EMBED_MAX_TOKENS;
    const int32_t limit = ends_capacity < cap ? ends_capacity : cap;
    char piece[256];
    int32_t end = 0;
    for (int32_t i = 0; i < limit; i++)
    {
        int32_t lstrip = 0;
        if (i == 0)
        {
            const int32_t probe = llama_token_to_piece(vocab, tokens[i], piece, sizeof piece, 0, true);
            if (probe > 0 && piece[0] == ' ')
            {
                lstrip = 1;
            }
        }

        const int32_t n = llama_token_to_piece(vocab, tokens[i], piece, sizeof piece, lstrip, true);
        if (n < 0)
        {
            free(tokens);
            return 3;
        }

        end += n;
        ends[i] = end;
    }

    free(tokens);
    *ends_len = limit;
    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_embed(const char * text, float * destination_768)
{
    if (g_model == NULL)
    {
        return 9;
    }

    if (text == NULL || destination_768 == NULL)
    {
        return 1;
    }

    if (recall_embed_output_width() != RECALL_EMBED_DIM)
    {
        return 7;
    }

    const struct llama_vocab * vocab = llama_model_get_vocab(g_model);
    const int32_t text_len = (int32_t) strlen(text);
    const int32_t n_neg = llama_tokenize(vocab, text, text_len, NULL, 0, true, true);
    if (n_neg == INT32_MIN || n_neg >= 0)
    {
        return 3;
    }

    const int32_t n_tokens = -n_neg;
    if (n_tokens > RECALL_EMBED_MAX_TOKENS)
    {
        return 4;
    }

    llama_token * tokens = (llama_token *) malloc((size_t) n_tokens * sizeof(llama_token));
    if (tokens == NULL)
    {
        return 5;
    }

    const int32_t n_written = llama_tokenize(vocab, text, text_len, tokens, n_tokens, true, true);
    if (n_written < 0)
    {
        free(tokens);
        return 3;
    }

    struct llama_batch batch = llama_batch_init(n_tokens, 0, 1);
    for (int32_t i = 0; i < n_tokens; i++)
    {
        batch.token[i] = tokens[i];
        batch.pos[i] = i;
        batch.n_seq_id[i] = 1;
        batch.seq_id[i][0] = 0;
        batch.logits[i] = 1;
    }

    batch.n_tokens = n_tokens;

    int32_t status;
    if (llama_model_has_encoder(g_model) && !llama_model_has_decoder(g_model))
    {
        status = llama_encode(g_ctx, batch);
    }
    else
    {
        status = llama_decode(g_ctx, batch);
    }

    llama_batch_free(batch);
    free(tokens);

    if (status != 0)
    {
        return 5;
    }

    const float * embedding = llama_get_embeddings_seq(g_ctx, 0);
    if (embedding == NULL)
    {
        return 6;
    }

    memcpy(destination_768, embedding, (size_t) RECALL_EMBED_DIM * sizeof(float));
    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_vision_open(const char * mmproj_path)
{
    if (mmproj_path == NULL)
    {
        return 1;
    }

    if (g_vision != NULL)
    {
        return 8;
    }

    if (g_model == NULL)
    {
        return 6;
    }

    struct mtmd_context_params params = mtmd_context_params_default();
    params.warmup = false;
    g_vision = mtmd_init_from_file(mmproj_path, g_model, params);
    if (g_vision == NULL)
    {
        return 2;
    }

    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_vision_close(void)
{
    if (g_vision != NULL)
    {
        mtmd_free(g_vision);
        g_vision = NULL;
    }

    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_audio_open(const char * mmproj_path)
{
    if (mmproj_path == NULL)
    {
        return 1;
    }

    if (g_audio != NULL)
    {
        return 8;
    }

    if (g_model == NULL)
    {
        return 6;
    }

    if (g_vision != NULL)
    {
        return 10;
    }

    struct mtmd_context_params params = mtmd_context_params_default();
    params.warmup = false;
    g_audio = mtmd_init_from_file(mmproj_path, g_model, params);
    if (g_audio == NULL)
    {
        return 2;
    }

    if (!mtmd_support_audio(g_audio) || mtmd_get_audio_sample_rate(g_audio) != RECALL_EMBED_AUDIO_SAMPLE_RATE)
    {
        mtmd_free(g_audio);
        g_audio = NULL;
        return 3;
    }

    return 0;
}

RECALL_EMBED_EXPORT void recall_embed_audio_close(void)
{
    if (g_audio != NULL)
    {
        mtmd_free(g_audio);
        g_audio = NULL;
    }
}

RECALL_EMBED_EXPORT int recall_embed_audio_embed(const float * samples, int32_t n_samples, float * out768)
{
    if (samples == NULL || n_samples <= 0 || out768 == NULL)
    {
        return 1;
    }

    mtmd_context * audio_ctx = g_audio != NULL ? g_audio : g_vision;
    if (audio_ctx == NULL)
    {
        return 4;
    }

    if (recall_embed_output_width() != RECALL_EMBED_DIM)
    {
        return 5;
    }

    mtmd_bitmap * bitmap = mtmd_bitmap_init_from_audio((size_t) n_samples, samples);
    if (bitmap == NULL)
    {
        return 2;
    }

    mtmd_input_chunks * chunks = mtmd_input_chunks_init();
    if (chunks == NULL)
    {
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const char * marker = mtmd_get_marker(audio_ctx);
    if (marker == NULL)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    struct mtmd_input_text text;
    text.text = marker;
    text.text_len = strlen(marker);
    text.add_special = true;
    text.parse_special = true;

    const mtmd_bitmap * bitmaps[1] = { bitmap };
    if (mtmd_tokenize(audio_ctx, chunks, &text, bitmaps, 1) != 0)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const mtmd_input_chunk * audio_chunk = NULL;
    const size_t chunk_count = mtmd_input_chunks_size(chunks);
    for (size_t i = 0; i < chunk_count; i++)
    {
        const mtmd_input_chunk * chunk = mtmd_input_chunks_get(chunks, i);
        if (chunk != NULL && mtmd_input_chunk_get_type(chunk) == MTMD_INPUT_CHUNK_TYPE_AUDIO)
        {
            audio_chunk = chunk;
            break;
        }
    }

    if (audio_chunk == NULL)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    if (mtmd_encode_chunk(audio_ctx, audio_chunk) != 0)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const float * embedding = mtmd_get_output_embd(audio_ctx);
    if (embedding == NULL)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const size_t n_tokens = mtmd_input_chunk_get_n_tokens(audio_chunk);
    if (n_tokens == 0)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    for (int32_t column = 0; column < RECALL_EMBED_DIM; column++)
    {
        double sum = 0.0;
        for (size_t token = 0; token < n_tokens; token++)
        {
            sum += (double) embedding[token * (size_t) RECALL_EMBED_DIM + (size_t) column];
        }

        out768[column] = (float) (sum / (double) n_tokens);
    }

    double norm = 0.0;
    for (int32_t column = 0; column < RECALL_EMBED_DIM; column++)
    {
        norm += (double) out768[column] * (double) out768[column];
    }

    mtmd_input_chunks_free(chunks);
    mtmd_bitmap_free(bitmap);

    if (norm <= 0.0)
    {
        return 2;
    }

    const double length = sqrt(norm);
    for (int32_t column = 0; column < RECALL_EMBED_DIM; column++)
    {
        out768[column] = (float) ((double) out768[column] / length);
    }

    return 0;
}

RECALL_EMBED_EXPORT int recall_embed_vision_embed(const unsigned char * jpeg, int32_t jpeg_len, float * out768)
{
    if (jpeg == NULL || jpeg_len <= 0 || out768 == NULL)
    {
        return 1;
    }

    if (g_vision == NULL)
    {
        return 4;
    }

    if (recall_embed_output_width() != RECALL_EMBED_DIM)
    {
        return 5;
    }

    struct mtmd_helper_init_opt opt = mtmd_helper_init_opt_default();
    struct mtmd_helper_bitmap_wrapper wrapper = mtmd_helper_bitmap_init_from_buf(g_vision, jpeg, (size_t) jpeg_len, false, opt);
    if (wrapper.bitmap == NULL)
    {
        return 2;
    }

    mtmd_bitmap * bitmap = wrapper.bitmap;
    mtmd_input_chunks * chunks = mtmd_input_chunks_init();
    if (chunks == NULL)
    {
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const char * marker = mtmd_get_marker(g_vision);
    if (marker == NULL)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    struct mtmd_input_text text;
    text.text = marker;
    text.text_len = strlen(marker);
    text.add_special = true;
    text.parse_special = true;

    const mtmd_bitmap * bitmaps[1] = { bitmap };
    if (mtmd_tokenize(g_vision, chunks, &text, bitmaps, 1) != 0)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const mtmd_input_chunk * image_chunk = NULL;
    const size_t chunk_count = mtmd_input_chunks_size(chunks);
    for (size_t i = 0; i < chunk_count; i++)
    {
        const mtmd_input_chunk * chunk = mtmd_input_chunks_get(chunks, i);
        if (chunk != NULL && mtmd_input_chunk_get_type(chunk) == MTMD_INPUT_CHUNK_TYPE_IMAGE)
        {
            image_chunk = chunk;
            break;
        }
    }

    if (image_chunk == NULL)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    if (mtmd_encode_chunk(g_vision, image_chunk) != 0)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const float * embedding = mtmd_get_output_embd(g_vision);
    if (embedding == NULL)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    const size_t n_tokens = mtmd_input_chunk_get_n_tokens(image_chunk);
    if (n_tokens == 0)
    {
        mtmd_input_chunks_free(chunks);
        mtmd_bitmap_free(bitmap);
        return 2;
    }

    for (int32_t column = 0; column < RECALL_EMBED_DIM; column++)
    {
        double sum = 0.0;
        for (size_t token = 0; token < n_tokens; token++)
        {
            sum += (double) embedding[token * (size_t) RECALL_EMBED_DIM + (size_t) column];
        }

        out768[column] = (float) (sum / (double) n_tokens);
    }

    double norm = 0.0;
    for (int32_t column = 0; column < RECALL_EMBED_DIM; column++)
    {
        norm += (double) out768[column] * (double) out768[column];
    }

    mtmd_input_chunks_free(chunks);
    mtmd_bitmap_free(bitmap);

    if (norm <= 0.0)
    {
        return 2;
    }

    const double length = sqrt(norm);
    for (int32_t column = 0; column < RECALL_EMBED_DIM; column++)
    {
        out768[column] = (float) ((double) out768[column] / length);
    }

    return 0;
}
