using System.Text;

namespace SortingLab.Core.Text;

public enum TextLanguage
{
    English,
    Russian,
    Mixed
}

/// <summary>
/// Генератор текстов из встроенного словаря. Одинаковый seed и размер
/// дают одинаковый текст для воспроизводимых экспериментов.
/// </summary>
public static class TextGenerator
{
    private static readonly string[] EnglishWords =
        ("algorithm data program code array list stack queue tree graph node edge table file " +
         "sort search compare swap merge pivot bucket count value index key record field " +
         "number text word letter symbol line column row matrix vector memory speed time " +
         "system method function object class module result input output process step range " +
         "order sequence structure analysis experiment measure benchmark random sample size " +
         "logic rule model task project language computer science student teacher lesson " +
         "book note idea problem solution answer question example test error debug build " +
         "run start finish quick simple natural external internal binary linear stable " +
         "unique repeat frequency dictionary generate load save open close read write " +
         "small large fast slow first last next previous left right upper lower " +
         "morning evening today tomorrow school city country world history physics music " +
         "friend family window door street river forest garden season winter summer spring autumn").Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

    private static readonly string[] RussianWords =
        ("алгоритм данные программа код массив список стек очередь дерево граф узел ребро " +
         "таблица файл сортировка поиск сравнение перестановка слияние опора корзина подсчёт " +
         "значение индекс ключ запись поле число текст слово буква символ строка столбец " +
         "матрица вектор память скорость время система метод функция объект класс модуль " +
         "результат вход выход процесс шаг диапазон порядок последовательность структура " +
         "анализ эксперимент измерение выборка размер логика правило модель задача проект " +
         "язык компьютер наука студент учитель урок книга заметка идея проблема решение " +
         "ответ вопрос пример тест ошибка сборка запуск начало конец быстрый простой " +
         "естественный внешний внутренний двоичный линейный устойчивый уникальный повтор " +
         "частота словарь генерация загрузка сохранение открытие чтение запись малый большой " +
         "быстрый медленный первый последний следующий предыдущий левый правый верхний нижний " +
         "утро вечер сегодня завтра школа город страна мир история физика музыка друг семья " +
         "окно дверь улица река лес сад время года зима лето весна осень").Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

    public static IReadOnlyList<string> GetVocabulary(TextLanguage language) => language switch
    {
        TextLanguage.English => EnglishWords,
        TextLanguage.Russian => RussianWords,
        _ => EnglishWords.Concat(RussianWords).ToArray()
    };

    public static string Generate(int wordCount, int seed, TextLanguage language = TextLanguage.English)
    {
        if (wordCount <= 0)
        {
            return string.Empty;
        }

        var random = new Random(seed);
        var builder = new StringBuilder(wordCount * 7);
        var sentenceStart = true;

        for (var i = 0; i < wordCount; i++)
        {
            var vocabulary = language switch
            {
                TextLanguage.English => EnglishWords,
                TextLanguage.Russian => RussianWords,
                _ => random.Next(2) == 0 ? EnglishWords : RussianWords
            };

            var word = vocabulary[random.Next(vocabulary.Length)];
            if (sentenceStart)
            {
                word = char.ToUpperInvariant(word[0]) + word[1..];
                sentenceStart = false;
            }

            builder.Append(word);

            var punctuation = random.NextDouble();
            if (punctuation < 0.08)
            {
                builder.Append(". ");
                sentenceStart = true;
            }
            else if (punctuation < 0.18)
            {
                builder.Append(", ");
            }
            else if (punctuation < 0.21)
            {
                builder.Append("; ");
            }
            else if (punctuation < 0.235)
            {
                builder.Append("! ");
                sentenceStart = true;
            }
            else if (punctuation < 0.26)
            {
                builder.Append("? ");
                sentenceStart = true;
            }
            else if (punctuation < 0.285)
            {
                builder.Append(".\n\n");
                sentenceStart = true;
            }
            else
            {
                builder.Append(' ');
            }
        }

        builder.Append('.');
        return builder.ToString();
    }
}
