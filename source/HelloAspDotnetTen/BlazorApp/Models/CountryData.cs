namespace BlazorApp.Models;

/// <summary>
/// Represents a country with various statistics for comparison games.
/// Nullable properties indicate data that may not be available for all countries.
/// </summary>
public record CountryData
{
    public required string Name { get; init; }
    public required string IsoCode2 { get; init; }
    public required string IsoCode3 { get; init; }
    
    /// <summary>Area in square kilometers</summary>
    public long? AreaSquareKm { get; init; }
    
    /// <summary>Population (2023/2024 estimates)</summary>
    public long? Population { get; init; }
    
    /// <summary>GDP in millions of USD (nominal, 2023)</summary>
    public long? GdpMillionsUsd { get; init; }
    
    /// <summary>GDP per capita in USD (nominal, 2023)</summary>
    public int? GdpPerCapitaUsd { get; init; }
    
    /// <summary>GNI per capita in PPP terms (international dollars, 2023)</summary>
    public int? GniPerCapitaPpp { get; init; }
    
    /// <summary>Population density (people per square km)</summary>
    public double? PopulationDensity { get; init; }
    
    /// <summary>Adult literacy rate (% of population 15+)</summary>
    public double? LiteracyRate { get; init; }
    
    /// <summary>Human Development Index (0-1 scale, 2023)</summary>
    public double? Hdi { get; init; }
    
    /// <summary>Life expectancy at birth (years)</summary>
    public double? LifeExpectancy { get; init; }
    
    /// <summary>Continent/Region</summary>
    public string? Continent { get; init; }
    
    /// <summary>Flag emoji for display</summary>
    public string? FlagEmoji { get; init; }
}

/// <summary>
/// Defines a type of comparison question for countries.
/// </summary>
public record CountryComparisonQuestion
{
    public required string Id { get; init; }
    public required string QuestionTemplate { get; init; }
    public required string PropertyName { get; init; }
    public required Func<CountryData, double?> GetValue { get; init; }
    public required string Unit { get; init; }
    public required string FormatString { get; init; }
    
    /// <summary>
    /// Whether higher values are "better" (for display purposes)
    /// </summary>
    public bool HigherIsBetter { get; init; } = true;
    
    public string GetFormattedValue(CountryData country)
    {
        var value = GetValue(country);
        return value.HasValue ? string.Format(FormatString, value.Value) : "N/A";
    }
    
    /// <summary>
    /// Checks if a country has data for this question
    /// </summary>
    public bool HasDataFor(CountryData country) => GetValue(country).HasValue;
}

/// <summary>
/// Represents the result of a single country comparison round.
/// </summary>
public record CountryComparisonResult
{
    public required CountryData Country1 { get; init; }
    public required CountryData Country2 { get; init; }
    public required CountryComparisonQuestion Question { get; init; }
    public required CountryData CorrectAnswer { get; init; }
    public required CountryData UserChoice { get; init; }
    public bool IsCorrect => CorrectAnswer == UserChoice;
}

/// <summary>
/// Tracks the user's score during a country comparison game session.
/// </summary>
public class CountryGameScore
{
    public int CorrectAnswers { get; private set; }
    public int TotalQuestions { get; private set; }
    public List<CountryComparisonResult> History { get; } = [];
    
    public double PercentageCorrect => TotalQuestions > 0 
        ? (double)CorrectAnswers / TotalQuestions * 100 
        : 0;
    
    public int CurrentStreak { get; private set; }
    public int BestStreak { get; private set; }
    
    public void RecordAnswer(CountryComparisonResult result)
    {
        TotalQuestions++;
        if (result.IsCorrect)
        {
            CorrectAnswers++;
            CurrentStreak++;
            if (CurrentStreak > BestStreak) BestStreak = CurrentStreak;
        }
        else
        {
            CurrentStreak = 0;
        }
        History.Add(result);
    }
    
    public void Reset()
    {
        CorrectAnswers = 0;
        TotalQuestions = 0;
        CurrentStreak = 0;
        BestStreak = 0;
        History.Clear();
    }
}
