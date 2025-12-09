namespace BlazorApp.Models;

/// <summary>
/// Represents a US state with various statistics for comparison games.
/// Designed to be extensible - add new properties as needed.
/// </summary>
public record StateData
{
    public required string Name { get; init; }
    public required string Abbreviation { get; init; }
    
    /// <summary>Area in square miles</summary>
    public required long AreaSquareMiles { get; init; }
    
    /// <summary>Population (2023 estimates)</summary>
    public required long Population { get; init; }
    
    /// <summary>Number of representatives in the House</summary>
    public required int HouseRepresentatives { get; init; }
    
    /// <summary>GDP in millions of dollars (2023)</summary>
    public long? GdpMillions { get; init; }
    
    /// <summary>Year admitted to the Union</summary>
    public int? YearAdmitted { get; init; }
    
    /// <summary>Image filename (without path) - e.g., "california.png"</summary>
    public string? ImageFileName { get; init; }
    
    /// <summary>Gets the image path for use in Blazor</summary>
    public string ImagePath => string.IsNullOrEmpty(ImageFileName) 
        ? "images/states/placeholder.png" 
        : $"images/states/{ImageFileName}";
}

/// <summary>
/// Defines a type of comparison question that can be asked.
/// </summary>
public record ComparisonQuestion
{
    public required string Id { get; init; }
    public required string QuestionTemplate { get; init; }
    public required string PropertyName { get; init; }
    public required Func<StateData, long?> GetValue { get; init; }
    public required string Unit { get; init; }
    public required string FormatString { get; init; }
    
    public string GetFormattedValue(StateData state)
    {
        var value = GetValue(state);
        return value.HasValue ? string.Format(FormatString, value.Value) : "N/A";
    }
}

/// <summary>
/// Represents the result of a single comparison round.
/// </summary>
public record ComparisonResult
{
    public required StateData State1 { get; init; }
    public required StateData State2 { get; init; }
    public required ComparisonQuestion Question { get; init; }
    public required StateData CorrectAnswer { get; init; }
    public required StateData UserChoice { get; init; }
    public bool IsCorrect => CorrectAnswer == UserChoice;
}

/// <summary>
/// Tracks the user's score during a game session.
/// </summary>
public class GameScore
{
    public int CorrectAnswers { get; private set; }
    public int TotalQuestions { get; private set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public List<ComparisonResult> History { get; } = [];

    public double PercentageCorrect => TotalQuestions > 0
        ? (double)CorrectAnswers / TotalQuestions * 100
        : 0;

    public void RecordAnswer(ComparisonResult result)
    {
        TotalQuestions++;
        if (result.IsCorrect) CorrectAnswers++;
        History.Add(result);
    }

    public void Reset()
    {
        CorrectAnswers = 0;
        TotalQuestions = 0;
        CurrentStreak = 0;
        BestStreak = 0;
    }

    // Helper methods for streak management
    public void IncrementCorrect()
    {
        CorrectAnswers++;
        TotalQuestions++;
        CurrentStreak++;
        if (CurrentStreak > BestStreak)
            BestStreak = CurrentStreak;
    }

    public void IncrementWrong()
    {
        TotalQuestions++;
        CurrentStreak = 0;
    }
}
