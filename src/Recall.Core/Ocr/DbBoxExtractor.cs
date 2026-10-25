namespace Recall.Core.Ocr;

public static class DbBoxExtractor
{
    private const double Threshold = 0.2;

    private const double MinScore = 0.45;

    private const double UnclipRatio = 1.4;

    private const int MinPixels = 10;

    private const int MaxBoxes = 3000;

    public static IReadOnlyList<OcrBox> Extract(float[] map, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (map.Length != width * height)
        {
            throw new ArgumentException("Map length must equal width times height.", nameof(map));
        }

        if (map.Length == 0)
        {
            return [];
        }

        var mask = new bool[width * height];
        for (var i = 0; i < map.Length; i++)
        {
            mask[i] = map[i] >= Threshold;
        }

        var components = new List<(int MinX, int MinY, int MaxX, int MaxY, int Count, double Sum)>();
        var visited = new bool[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                if (!mask[index] || visited[index])
                {
                    continue;
                }

                var minX = x;
                var maxX = x;
                var minY = y;
                var maxY = y;
                var count = 0;
                var sum = 0d;
                var stack = new Stack<int>();
                stack.Push(index);
                visited[index] = true;
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    var cx = current % width;
                    var cy = current / width;
                    count++;
                    sum += map[current];
                    if (cx < minX)
                    {
                        minX = cx;
                    }

                    if (cx > maxX)
                    {
                        maxX = cx;
                    }

                    if (cy < minY)
                    {
                        minY = cy;
                    }

                    if (cy > maxY)
                    {
                        maxY = cy;
                    }

                    TryPush(cx - 1, cy, width, height, mask, visited, stack);
                    TryPush(cx + 1, cy, width, height, mask, visited, stack);
                    TryPush(cx, cy - 1, width, height, mask, visited, stack);
                    TryPush(cx, cy + 1, width, height, mask, visited, stack);
                }

                if (count >= MinPixels)
                {
                    components.Add((minX, minY, maxX, maxY, count, sum));
                }
            }
        }

        var boxes = new List<OcrBox>();
        foreach (var (minX, minY, maxX, maxY, count, sum) in components)
        {
            var score = sum / count;
            if (score < MinScore)
            {
                continue;
            }

            var boxWidth = maxX - minX + 1;
            var boxHeight = maxY - minY + 1;
            var area = boxWidth * boxHeight;
            var expand = area * UnclipRatio / (2 * (boxWidth + boxHeight));
            var x0 = (int)Math.Floor(minX - expand);
            var y0 = (int)Math.Floor(minY - expand);
            var x1 = (int)Math.Ceiling(maxX + expand);
            var y1 = (int)Math.Ceiling(maxY + expand);
            x0 = Math.Clamp(x0, 0, width - 1);
            y0 = Math.Clamp(y0, 0, height - 1);
            x1 = Math.Clamp(x1, 0, width - 1);
            y1 = Math.Clamp(y1, 0, height - 1);
            boxes.Add(new OcrBox(x0, y0, x1 - x0 + 1, y1 - y0 + 1, score));
        }

        if (boxes.Count == 0)
        {
            return boxes;
        }

        boxes.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (boxes.Count > MaxBoxes)
        {
            boxes = boxes.Take(MaxBoxes).ToList();
        }

        boxes.Sort((a, b) =>
        {
            var y = a.Y.CompareTo(b.Y);
            return y != 0 ? y : a.X.CompareTo(b.X);
        });
        return boxes;
    }

    private static void TryPush(int x, int y, int width, int height, bool[] mask, bool[] visited, Stack<int> stack)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return;
        }

        var index = y * width + x;
        if (!mask[index] || visited[index])
        {
            return;
        }

        visited[index] = true;
        stack.Push(index);
    }
}
