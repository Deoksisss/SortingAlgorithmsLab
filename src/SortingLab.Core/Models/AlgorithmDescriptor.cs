namespace SortingLab.Core.Models;

/// <summary>
/// Описание алгоритма для информационной карточки в интерфейсе.
/// </summary>
public sealed record AlgorithmDescriptor(
    string Id,
    string Name,
    string Category,
    string ShortDescription,
    string Idea,
    string Principle,
    string TimeComplexity,
    string SpaceComplexity,
    string Features);
