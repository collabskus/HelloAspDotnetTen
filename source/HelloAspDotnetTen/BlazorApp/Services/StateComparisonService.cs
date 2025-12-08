using BlazorApp.Models;

namespace BlazorApp.Services;

/// <summary>
/// Service that manages state data and comparison game logic.
/// Registered as a scoped service to maintain score per user session.
/// </summary>
public class StateComparisonService
{
    private readonly List<StateData> _states;
    private readonly List<ComparisonQuestion> _questions;
    private readonly Random _random = new();
    
    public GameScore Score { get; } = new();
    
    public IReadOnlyList<StateData> States => _states;
    public IReadOnlyList<ComparisonQuestion> AvailableQuestions => _questions;
    
    public StateComparisonService()
    {
        _states = InitializeStates();
        _questions = InitializeQuestions();
    }
    
    /// <summary>
    /// Gets two random different states for comparison.
    /// </summary>
    public (StateData State1, StateData State2) GetRandomStatePair()
    {
        var index1 = _random.Next(_states.Count);
        int index2;
        do { index2 = _random.Next(_states.Count); } 
        while (index2 == index1);
        
        return (_states[index1], _states[index2]);
    }
    
    /// <summary>
    /// Gets a random question from available questions.
    /// </summary>
    public ComparisonQuestion GetRandomQuestion()
    {
        return _questions[_random.Next(_questions.Count)];
    }
    
    /// <summary>
    /// Determines which state has the higher value for the given question.
    /// </summary>
    public StateData GetCorrectAnswer(StateData state1, StateData state2, ComparisonQuestion question)
    {
        var value1 = question.GetValue(state1) ?? 0;
        var value2 = question.GetValue(state2) ?? 0;
        return value1 >= value2 ? state1 : state2;
    }
    
    /// <summary>
    /// Checks the user's answer and records the result.
    /// </summary>
    public ComparisonResult CheckAnswer(StateData state1, StateData state2, 
        ComparisonQuestion question, StateData userChoice)
    {
        var correct = GetCorrectAnswer(state1, state2, question);
        var result = new ComparisonResult
        {
            State1 = state1,
            State2 = state2,
            Question = question,
            CorrectAnswer = correct,
            UserChoice = userChoice
        };
        Score.RecordAnswer(result);
        return result;
    }
    
    private static List<ComparisonQuestion> InitializeQuestions()
    {
        return
        [
            new ComparisonQuestion
            {
                Id = "area",
                QuestionTemplate = "Which state is bigger by area?",
                PropertyName = nameof(StateData.AreaSquareMiles),
                GetValue = s => s.AreaSquareMiles,
                Unit = "square miles",
                FormatString = "{0:N0} sq mi"
            },
            new ComparisonQuestion
            {
                Id = "population",
                QuestionTemplate = "Which state has a larger population?",
                PropertyName = nameof(StateData.Population),
                GetValue = s => s.Population,
                Unit = "people",
                FormatString = "{0:N0}"
            },
            new ComparisonQuestion
            {
                Id = "representatives",
                QuestionTemplate = "Which state has more representatives in the House?",
                PropertyName = nameof(StateData.HouseRepresentatives),
                GetValue = s => s.HouseRepresentatives,
                Unit = "representatives",
                FormatString = "{0}"
            }
        ];
    }
    
    private static List<StateData> InitializeStates()
    {
        // All 50 US states with area (sq mi), population (2023 est), and House reps (2023)
        return
        [
            new() { Name = "Alabama", Abbreviation = "AL", AreaSquareMiles = 52420, Population = 5108468, HouseRepresentatives = 7 },
            new() { Name = "Alaska", Abbreviation = "AK", AreaSquareMiles = 665384, Population = 733391, HouseRepresentatives = 1 },
            new() { Name = "Arizona", Abbreviation = "AZ", AreaSquareMiles = 113990, Population = 7431344, HouseRepresentatives = 9 },
            new() { Name = "Arkansas", Abbreviation = "AR", AreaSquareMiles = 53179, Population = 3067732, HouseRepresentatives = 4 },
            new() { Name = "California", Abbreviation = "CA", AreaSquareMiles = 163695, Population = 38965193, HouseRepresentatives = 52 },
            new() { Name = "Colorado", Abbreviation = "CO", AreaSquareMiles = 104094, Population = 5877610, HouseRepresentatives = 8 },
            new() { Name = "Connecticut", Abbreviation = "CT", AreaSquareMiles = 5543, Population = 3617176, HouseRepresentatives = 5 },
            new() { Name = "Delaware", Abbreviation = "DE", AreaSquareMiles = 2489, Population = 1031890, HouseRepresentatives = 1 },
            new() { Name = "Florida", Abbreviation = "FL", AreaSquareMiles = 65758, Population = 22610726, HouseRepresentatives = 28 },
            new() { Name = "Georgia", Abbreviation = "GA", AreaSquareMiles = 59425, Population = 11029227, HouseRepresentatives = 14 },
            new() { Name = "Hawaii", Abbreviation = "HI", AreaSquareMiles = 10932, Population = 1435138, HouseRepresentatives = 2 },
            new() { Name = "Idaho", Abbreviation = "ID", AreaSquareMiles = 83569, Population = 1964726, HouseRepresentatives = 2 },
            new() { Name = "Illinois", Abbreviation = "IL", AreaSquareMiles = 57914, Population = 12549689, HouseRepresentatives = 17 },
            new() { Name = "Indiana", Abbreviation = "IN", AreaSquareMiles = 36420, Population = 6862199, HouseRepresentatives = 9 },
            new() { Name = "Iowa", Abbreviation = "IA", AreaSquareMiles = 56273, Population = 3207004, HouseRepresentatives = 4 },
            new() { Name = "Kansas", Abbreviation = "KS", AreaSquareMiles = 82278, Population = 2940546, HouseRepresentatives = 4 },
            new() { Name = "Kentucky", Abbreviation = "KY", AreaSquareMiles = 40408, Population = 4526154, HouseRepresentatives = 6 },
            new() { Name = "Louisiana", Abbreviation = "LA", AreaSquareMiles = 52378, Population = 4573749, HouseRepresentatives = 6 },
            new() { Name = "Maine", Abbreviation = "ME", AreaSquareMiles = 35380, Population = 1395722, HouseRepresentatives = 2 },
            new() { Name = "Maryland", Abbreviation = "MD", AreaSquareMiles = 12406, Population = 6180253, HouseRepresentatives = 8 },
            new() { Name = "Massachusetts", Abbreviation = "MA", AreaSquareMiles = 10554, Population = 7001399, HouseRepresentatives = 9 },
            new() { Name = "Michigan", Abbreviation = "MI", AreaSquareMiles = 96714, Population = 10037261, HouseRepresentatives = 13 },
            new() { Name = "Minnesota", Abbreviation = "MN", AreaSquareMiles = 86936, Population = 5737915, HouseRepresentatives = 8 },
            new() { Name = "Mississippi", Abbreviation = "MS", AreaSquareMiles = 48432, Population = 2939690, HouseRepresentatives = 4 },
            new() { Name = "Missouri", Abbreviation = "MO", AreaSquareMiles = 69707, Population = 6196156, HouseRepresentatives = 8 },
            new() { Name = "Montana", Abbreviation = "MT", AreaSquareMiles = 147040, Population = 1132812, HouseRepresentatives = 2 },
            new() { Name = "Nebraska", Abbreviation = "NE", AreaSquareMiles = 77348, Population = 1978379, HouseRepresentatives = 3 },
            new() { Name = "Nevada", Abbreviation = "NV", AreaSquareMiles = 110572, Population = 3194176, HouseRepresentatives = 4 },
            new() { Name = "New Hampshire", Abbreviation = "NH", AreaSquareMiles = 9349, Population = 1402054, HouseRepresentatives = 2 },
            new() { Name = "New Jersey", Abbreviation = "NJ", AreaSquareMiles = 8723, Population = 9290841, HouseRepresentatives = 12 },
            new() { Name = "New Mexico", Abbreviation = "NM", AreaSquareMiles = 121590, Population = 2114371, HouseRepresentatives = 3 },
            new() { Name = "New York", Abbreviation = "NY", AreaSquareMiles = 54555, Population = 19571216, HouseRepresentatives = 26 },
            new() { Name = "North Carolina", Abbreviation = "NC", AreaSquareMiles = 53819, Population = 10835491, HouseRepresentatives = 14 },
            new() { Name = "North Dakota", Abbreviation = "ND", AreaSquareMiles = 70698, Population = 783926, HouseRepresentatives = 1 },
            new() { Name = "Ohio", Abbreviation = "OH", AreaSquareMiles = 44826, Population = 11785935, HouseRepresentatives = 15 },
            new() { Name = "Oklahoma", Abbreviation = "OK", AreaSquareMiles = 69899, Population = 4053824, HouseRepresentatives = 5 },
            new() { Name = "Oregon", Abbreviation = "OR", AreaSquareMiles = 98379, Population = 4233358, HouseRepresentatives = 6 },
            new() { Name = "Pennsylvania", Abbreviation = "PA", AreaSquareMiles = 46054, Population = 12961683, HouseRepresentatives = 17 },
            new() { Name = "Rhode Island", Abbreviation = "RI", AreaSquareMiles = 1545, Population = 1095962, HouseRepresentatives = 2 },
            new() { Name = "South Carolina", Abbreviation = "SC", AreaSquareMiles = 32020, Population = 5373555, HouseRepresentatives = 7 },
            new() { Name = "South Dakota", Abbreviation = "SD", AreaSquareMiles = 77116, Population = 919318, HouseRepresentatives = 1 },
            new() { Name = "Tennessee", Abbreviation = "TN", AreaSquareMiles = 42144, Population = 7126489, HouseRepresentatives = 9 },
            new() { Name = "Texas", Abbreviation = "TX", AreaSquareMiles = 268596, Population = 30503301, HouseRepresentatives = 38 },
            new() { Name = "Utah", Abbreviation = "UT", AreaSquareMiles = 84897, Population = 3417734, HouseRepresentatives = 4 },
            new() { Name = "Vermont", Abbreviation = "VT", AreaSquareMiles = 9616, Population = 647464, HouseRepresentatives = 1 },
            new() { Name = "Virginia", Abbreviation = "VA", AreaSquareMiles = 42775, Population = 8683619, HouseRepresentatives = 11 },
            new() { Name = "Washington", Abbreviation = "WA", AreaSquareMiles = 71298, Population = 7812880, HouseRepresentatives = 10 },
            new() { Name = "West Virginia", Abbreviation = "WV", AreaSquareMiles = 24230, Population = 1770071, HouseRepresentatives = 2 },
            new() { Name = "Wisconsin", Abbreviation = "WI", AreaSquareMiles = 65496, Population = 5910955, HouseRepresentatives = 8 },
            new() { Name = "Wyoming", Abbreviation = "WY", AreaSquareMiles = 97813, Population = 584057, HouseRepresentatives = 1 }
        ];
    }
}
