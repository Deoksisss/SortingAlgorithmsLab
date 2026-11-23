using SortingLab.Core.Abstractions;
using SortingLab.Core.Execution;
using SortingLab.Core.Models;

namespace SortingLab.Core.Algorithms.Internal;

/// <summary>
/// Сортировка чётно-нечётными перестановками (Odd-Even Sort).
/// </summary>
public sealed class OddEvenSort : IArraySortAlgorithm
{
    public AlgorithmDescriptor Descriptor { get; } = new(
        Id: "odd-even",
        Name: "Odd-Even Sort",
        Category: "Квадратичный алгоритм",
        ShortDescription: "Улучшенная пузырьковая сортировка: соседние пары сравниваются в чётных и нечётных фазах.",
        Idea: "Массив проходится парами соседних элементов. За один проход выполняются чётная и нечётная фазы, после чего проход повторяется, пока есть перестановки.",
        Principle: "Если левый элемент пары больше правого, они меняются местами. Чётная фаза обрабатывает пары (0,1), (2,3), ..., нечётная — (1,2), (3,4), ...",
        TimeComplexity: "O(n²) в худшем и среднем случае",
        SpaceComplexity: "O(1) дополнительной памяти",
        Features: "Устойчив, прост для изучения, хорошо показывает постепенное «всплывание» больших элементов.");

    public async Task<ArraySortResult> SortAsync(
        IReadOnlyList<int> values,
        IStepSink<ArraySortStep> stepSink,
        CancellationToken cancellationToken = default)
    {
        var array = values.ToArray();
        var context = new ArraySortContext(array, stepSink);
        await context.InitialAsync(cancellationToken);

        if (array.Length <= 1)
        {
            await context.CompleteAsync(
                array,
                "Массив содержит не более одного элемента, поэтому он уже отсортирован.",
                cancellationToken);
            return new ArraySortResult((int[])array.Clone(), context.Statistics);
        }

        var sweep = 0;
        var swappedInSweep = true;

        while (swappedInSweep)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sweep++;
            swappedInSweep = false;

            await context.PhaseAsync(
                $"Проход {sweep}: чётная фаза",
                "Сравниваем пары (0, 1), (2, 3), (4, 5) и так далее. Это чётная фаза.",
                cancellationToken);

            for (var i = 0; i + 1 < array.Length; i += 2)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var left = array[i];
                var right = array[i + 1];
                var mustSwap = left > right;

                await context.CompareAsync(
                    i,
                    i + 1,
                    mustSwap
                        ? $"Сравниваем {left} и {right}. Левое число больше правого, поэтому их нужно поменять местами."
                        : $"Сравниваем {left} и {right}. Пара уже стоит в правильном порядке.",
                    cancellationToken);

                if (mustSwap)
                {
                    await context.SwapAsync(
                        i,
                        i + 1,
                        $"Меняем местами {left} и {right}: меньшее число должно стоять левее.",
                        cancellationToken);
                    swappedInSweep = true;
                }
            }

            await context.PhaseAsync(
                $"Проход {sweep}: нечётная фаза",
                "Теперь сравниваем пары (1, 2), (3, 4), (5, 6) и так далее. Это нечётная фаза.",
                cancellationToken);

            for (var i = 1; i + 1 < array.Length; i += 2)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var left = array[i];
                var right = array[i + 1];
                var mustSwap = left > right;

                await context.CompareAsync(
                    i,
                    i + 1,
                    mustSwap
                        ? $"Сравниваем {left} и {right}. Левое число больше правого, поэтому их нужно поменять местами."
                        : $"Сравниваем {left} и {right}. Пара уже стоит в правильном порядке.",
                    cancellationToken);

                if (mustSwap)
                {
                    await context.SwapAsync(
                        i,
                        i + 1,
                        $"Меняем местами {left} и {right}: меньшее число должно стоять левее.",
                        cancellationToken);
                    swappedInSweep = true;
                }
            }

            await context.PassCompletedAsync(
                sweep,
                swappedInSweep,
                swappedInSweep
                    ? $"Проход {sweep} завершён. Перестановки были, поэтому нужен ещё один проход."
                    : $"Проход {sweep} завершён. Перестановок не было — массив отсортирован.",
                cancellationToken);
        }

        await context.CompleteAsync(
            array,
            $"Сортировка завершена за {sweep} проход(ов). Массив упорядочен по возрастанию.",
            cancellationToken);

        return new ArraySortResult((int[])array.Clone(), context.Statistics);
    }
}
