using Tokenizers.DotNet;

namespace Recall.Adapters.Embedding;

internal static class HuggingFaceTokenizerFactory
{
    internal static Tokenizer Create(string tokenizerJsonPath) => new(vocabPath: tokenizerJsonPath);
}
