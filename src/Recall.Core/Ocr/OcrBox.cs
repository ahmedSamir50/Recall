namespace Recall.Core.Ocr;

public readonly record struct OcrBox(int X, int Y, int Width, int Height, double Score);
