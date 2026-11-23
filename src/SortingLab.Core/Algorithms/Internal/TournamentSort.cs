using SortingLab.Core.Abstractions;
using SortingLab.Core.Execution;
using SortingLab.Core.Models;

namespace SortingLab.Core.Algorithms.Internal;

/// <summary>
/// Сортировка турнирным деревом. Внутренние узлы хранят индексы
/// победителей (минимальных элементов) своих поддеревьев.
/// </summary>
public sealed class TournamentSort : IArraySortAlgorithm
{
    public AlgorithmDescriptor Descriptor { get; } = new(
        Id: "tournament",
        Name: "Tournament Sort",
        Category: "Усовершенствованный алгоритм",
        ShortDescription: "Турнирное дерево последовательно выбирает минимум и обновляет только путь к корню.",
        Idea: "Элементы образуют листья бинарного дерева. В каждом внутреннем узле хранится индекс меньшего из двух потомков. Корень всегда указывает на текущий минимум.",
        Principle: "Минимум переносится в результат, его лист исключается, после чего пересчитываются победители только на пути от этого листа к корню.",
        TimeComplexity: "O(n log n) сравнений",
        SpaceComplexity: "O(n) на дерево и результат",
        Features: "Наглядно показывает турнирный принцип выбора; не сортирует массив на месте.");

    public async Task<ArraySortResult> SortAsync(
        IReadOnlyList<int> values,
        IStepSink<ArraySortStep> stepSink,
        CancellationToken cancellationToken = default)
    {
        var source = values.ToArray();
        var context = new ArraySortContext(source, stepSink);
        await context.InitialAsync(cancellationToken);

        if (source.Length == 0)
        {
            await context.CompleteAsync(
                Array.Empty<int>(),
                "Массив пуст: результирующий массив тоже пуст.",
                cancellationToken);
            return new ArraySortResult(Array.Empty<int>(), context.Statistics);
        }

        var leafCount = 1;
        while (leafCount < source.Length)
        {
            leafCount <<= 1;
        }

        var tree = new int[2 * leafCount];
        Array.Fill(tree, -1);
        for (var i = 0; i < source.Length; i++)
        {
            tree[leafCount + i] = i;
        }

        await context.PhaseAsync(
            "Построение турнирного дерева",
            "Строим дерево снизу вверх. В каждом узле будет храниться индекс меньшего из двух потомков.",
            cancellationToken);

        for (var node = leafCount - 1; node >= 1; node--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UpdateNodeAsync(context, tree, source, node, Array.Empty<int>(), true, cancellationToken);
        }

        await context.TreeAsync(
            CreateSnapshot(tree, source, leafCount, Array.Empty<int>()),
            "Дерево построено",
            $"В корне дерева находится элемент с индексом {tree[1]} и значением {source[tree[1]]}. Это минимум всего массива.",
            cancellationToken);

        var result = new int[source.Length];

        for (var outputIndex = 0; outputIndex < result.Length; outputIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var winner = tree[1];
            if (winner < 0)
            {
                break;
            }

            var value = source[winner];
            var leafNode = leafCount + winner;
            var activePath = BuildPath(leafNode, 1);

            await context.SelectTournamentAsync(
                winner,
                CreateSnapshot(tree, source, leafCount, activePath),
                $"Победитель турнира — элемент с индексом {winner} и значением {value}. Это минимальный элемент среди оставшихся.",
                cancellationToken);

            result[outputIndex] = value;
            await context.WriteResultAsync(
                outputIndex,
                winner,
                value,
                $"Переносим значение {value} в результат на позицию {outputIndex}.",
                cancellationToken);

            tree[leafNode] = -1;
            await context.TreeAsync(
                CreateSnapshot(tree, source, leafCount, BuildPath(leafNode, 1)),
                "Лист исключён",
                $"Элемент с индексом {winner} больше не участвует в турнире: его лист помечен как исключённый.",
                cancellationToken);

            for (var node = leafNode / 2; node >= 1; node /= 2)
            {
                await UpdateNodeAsync(
                    context,
                    tree,
                    source,
                    node,
                    BuildPath(node, 1),
                    false,
                    cancellationToken);
            }
        }

        await context.CompleteAsync(
            result,
            "Все элементы перенесены в результат в порядке возрастания. Турнирное дерево завершило работу.",
            cancellationToken);

        return new ArraySortResult((int[])result.Clone(), context.Statistics);
    }

    private static async ValueTask UpdateNodeAsync(
        ArraySortContext context,
        int[] tree,
        int[] source,
        int node,
        IReadOnlyList<int> activePath,
        bool isBuilding,
        CancellationToken cancellationToken)
    {
        var left = tree[node * 2];
        var right = tree[node * 2 + 1];
        int winner;
        string explanation;

        if (left >= 0 && right >= 0)
        {
            context.CountComparison();
            winner = source[left] <= source[right] ? left : right;
            explanation = isBuilding
                ? $"Сравниваем победителей потомков: индекс {left} (значение {source[left]}) и индекс {right} (значение {source[right]}). Победитель — {winner}."
                : $"После исключения элемента обновляем узел: слева индекс {left} (значение {source[left]}), справа индекс {right} (значение {source[right]}). Победитель — {winner}.";
        }
        else if (left >= 0)
        {
            winner = left;
            explanation = $"Правый потомок пуст, поэтому победитель узла — элемент с индексом {left}.";
        }
        else if (right >= 0)
        {
            winner = right;
            explanation = $"Левый потомок пуст, поэтому победитель узла — элемент с индексом {right}.";
        }
        else
        {
            winner = -1;
            explanation = "Оба потомка исключены, поэтому узел тоже становится пустым.";
        }

        tree[node] = winner;
        await context.TreeAsync(
            CreateSnapshot(tree, source, tree.Length / 2, activePath),
            isBuilding ? "Построение узла дерева" : "Обновление пути",
            explanation,
            cancellationToken);
    }

    private static IReadOnlyList<int> BuildPath(int node, int root)
    {
        var path = new List<int>();
        var current = node;
        while (current >= root)
        {
            path.Add(current);
            if (current == root)
            {
                break;
            }

            current /= 2;
        }

        path.Reverse();
        return path;
    }

    private static TournamentSnapshot CreateSnapshot(
        int[] tree,
        int[] source,
        int leafCount,
        IReadOnlyList<int> activePath)
    {
        var nodes = new TournamentNode[tree.Length];
        nodes[0] = new TournamentNode(0, null, null, false, false);

        for (var node = 1; node < tree.Length; node++)
        {
            var winner = tree[node];
            var isLeaf = node >= leafCount;
            var value = winner >= 0 ? source[winner] : (int?)null;
            var active = winner >= 0;
            nodes[node] = new TournamentNode(
                node,
                winner >= 0 ? winner : null,
                value,
                isLeaf,
                active);
        }

        return new TournamentSnapshot(
            leafCount,
            source.Length,
            nodes,
            tree[1] >= 0 ? tree[1] : null,
            activePath);
    }
}
