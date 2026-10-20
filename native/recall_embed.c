#include "llama.h"

#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#if defined(_WIN32)
#define RECALL_EMBED_EXPORT __declspec(dllexport)
#else
#define RECALL_EMBED_EXPORT __attribute__((visibility("default")))
#endif

static struct llama_model * g_model = NULL;
static struct llama_context * g_ctx = NULL;
static bool g_backend_initialized = false;

#define RECALL_EMBED_DIM 768
#define RECALL_EMBED_MAX_TOKENS 8192

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
