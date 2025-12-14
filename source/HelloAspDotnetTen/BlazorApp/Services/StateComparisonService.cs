using BlazorApp.Models;

namespace BlazorApp.Services;

/// <summary>
/// Service for managing US state comparison game logic.
/// </summary>
public class StateComparisonService
{
    private readonly List<StateData> _states;
    private readonly List<ComparisonQuestion> _questions;
    private readonly Random _random = new();

    public IReadOnlyList<StateData> States => _states;
    public IReadOnlyList<ComparisonQuestion> Questions => _questions;
    public GameScore Score { get; } = new();

    public StateComparisonService()
    {
        _states = InitializeStates();
        _questions = InitializeQuestions();
    }

    /// <summary>
    /// Gets a random pair of states where their values for the given question are NOT equal.
    /// This prevents "gotcha" questions where both states have the same value.
    /// </summary>
    public (StateData State1, StateData State2) GetRandomStatePairWithoutTie(ComparisonQuestion question)
    {
        // Filter to states that have data for this question
        var validStates = _states.Where(s => question.GetValue(s).HasValue).ToList();
        
        if (validStates.Count < 2)
        {
            throw new InvalidOperationException($"Not enough states with data for question: {question.Id}");
        }

        // Try to find a pair without equal values (max 100 attempts to avoid infinite loop)
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var state1 = validStates[_random.Next(validStates.Count)];
            var state2 = validStates[_random.Next(validStates.Count)];

            // Ensure different states
            if (state1 == state2) continue;

            var value1 = question.GetValue(state1);
            var value2 = question.GetValue(state2);

            // Ensure values are NOT equal (prevents gotcha questions)
            if (value1 != value2)
            {
                return (state1, state2);
            }
        }

        // Fallback: just return any two different states (shouldn't happen often)
        var fallback1 = validStates[0];
        var fallback2 = validStates[1];
        return (fallback1, fallback2);
    }

    /// <summary>
    /// Gets a random pair of states (original method, may have ties).
    /// </summary>
    public (StateData State1, StateData State2) GetRandomStatePair()
    {
        var state1 = _states[_random.Next(_states.Count)];
        StateData state2;
        do
        {
            state2 = _states[_random.Next(_states.Count)];
        } while (state2 == state1);

        return (state1, state2);
    }

    /// <summary>
    /// Gets a random question type.
    /// </summary>
    public ComparisonQuestion GetRandomQuestion()
    {
        return _questions[_random.Next(_questions.Count)];
    }

    /// <summary>
    /// Determines the correct answer for a comparison.
    /// </summary>
    public StateData GetCorrectAnswer(StateData state1, StateData state2, ComparisonQuestion question)
    {
        var value1 = question.GetValue(state1) ?? 0;
        var value2 = question.GetValue(state2) ?? 0;
        return value1 > value2 ? state1 : state2;
    }

    /// <summary>
    /// Checks the user's answer and records the result.
    /// </summary>
    public ComparisonResult CheckAnswer(StateData state1, StateData state2,
        ComparisonQuestion question, StateData userChoice)
    {
        var correct = GetCorrectAnswer(state1, state2, question);
        var isCorrect = correct == userChoice;
        
        var result = new ComparisonResult
        {
            State1 = state1,
            State2 = state2,
            Question = question,
            CorrectAnswer = correct,
            UserChoice = userChoice
        };

        // Update score with streak tracking
        Score.TotalQuestions++;
        if (isCorrect)
        {
            Score.CorrectAnswers++;
            Score.CurrentStreak++;
            if (Score.CurrentStreak > Score.BestStreak)
            {
                Score.BestStreak = Score.CurrentStreak;
            }
        }
        else
        {
            Score.CurrentStreak = 0;
        }
        
        Score.History.Add(result);
        return result;
    }

    private static List<ComparisonQuestion> InitializeQuestions()
    {
        return
        [
            new ComparisonQuestion
            {
                Id = "area",
                QuestionTemplate = "Which state is larger by area?",
                PropertyName = nameof(StateData.AreaSquareMiles),
                GetValue = s => s.AreaSquareMiles,
                Unit = "sq mi",
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
        return
        [
            new StateData { Name = "Alabama", Abbreviation = "AL", AreaSquareMiles = 52420, Population = 5024279, HouseRepresentatives = 7 },
            new StateData { Name = "Alaska", Abbreviation = "AK", AreaSquareMiles = 665384, Population = 733391, HouseRepresentatives = 1 },
            new StateData { Name = "Arizona", Abbreviation = "AZ", AreaSquareMiles = 113990, Population = 7151502, HouseRepresentatives = 9 },
            new StateData { Name = "Arkansas", Abbreviation = "AR", AreaSquareMiles = 53179, Population = 3011524, HouseRepresentatives = 4 },
            new StateData { Name = "California", Abbreviation = "CA", AreaSquareMiles = 163695, Population = 39538223, HouseRepresentatives = 52 },
            new StateData { Name = "Colorado", Abbreviation = "CO", AreaSquareMiles = 104094, Population = 5773714, HouseRepresentatives = 8 },
            new StateData { Name = "Connecticut", Abbreviation = "CT", AreaSquareMiles = 5543, Population = 3605944, HouseRepresentatives = 5 },
            new StateData { Name = "Delaware", Abbreviation = "DE", AreaSquareMiles = 2489, Population = 989948, HouseRepresentatives = 1 },
            new StateData { Name = "Florida", Abbreviation = "FL", AreaSquareMiles = 65758, Population = 21538187, HouseRepresentatives = 28 },
            new StateData { Name = "Georgia", Abbreviation = "GA", AreaSquareMiles = 59425, Population = 10711908, HouseRepresentatives = 14 },
            new StateData { Name = "Hawaii", Abbreviation = "HI", AreaSquareMiles = 10932, Population = 1455271, HouseRepresentatives = 2 },
            new StateData { Name = "Idaho", Abbreviation = "ID", AreaSquareMiles = 83569, Population = 1839106, HouseRepresentatives = 2 },
            new StateData { Name = "Illinois", Abbreviation = "IL", AreaSquareMiles = 57914, Population = 12812508, HouseRepresentatives = 17 },
            new StateData { Name = "Indiana", Abbreviation = "IN", AreaSquareMiles = 36420, Population = 6785528, HouseRepresentatives = 9 },
            new StateData { Name = "Iowa", Abbreviation = "IA", AreaSquareMiles = 56273, Population = 3190369, HouseRepresentatives = 4 },
            new StateData { Name = "Kansas", Abbreviation = "KS", AreaSquareMiles = 82278, Population = 2937880, HouseRepresentatives = 4 },
            new StateData { Name = "Kentucky", Abbreviation = "KY", AreaSquareMiles = 40408, Population = 4505836, HouseRepresentatives = 6 },
            new StateData { Name = "Louisiana", Abbreviation = "LA", AreaSquareMiles = 52378, Population = 4657757, HouseRepresentatives = 6 },
            new StateData { Name = "Maine", Abbreviation = "ME", AreaSquareMiles = 35380, Population = 1362359, HouseRepresentatives = 2 },
            new StateData { Name = "Maryland", Abbreviation = "MD", AreaSquareMiles = 12406, Population = 6177224, HouseRepresentatives = 8 },
            new StateData { Name = "Massachusetts", Abbreviation = "MA", AreaSquareMiles = 10554, Population = 7029917, HouseRepresentatives = 9 },
            new StateData { Name = "Michigan", Abbreviation = "MI", AreaSquareMiles = 96714, Population = 10077331, HouseRepresentatives = 13 },
            new StateData { Name = "Minnesota", Abbreviation = "MN", AreaSquareMiles = 86936, Population = 5706494, HouseRepresentatives = 8 },
            new StateData { Name = "Mississippi", Abbreviation = "MS", AreaSquareMiles = 48432, Population = 2961279, HouseRepresentatives = 4 },
            new StateData { Name = "Missouri", Abbreviation = "MO", AreaSquareMiles = 69707, Population = 6154913, HouseRepresentatives = 8 },
            new StateData { Name = "Montana", Abbreviation = "MT", AreaSquareMiles = 147040, Population = 1084225, HouseRepresentatives = 2 },
            new StateData { Name = "Nebraska", Abbreviation = "NE", AreaSquareMiles = 77348, Population = 1961504, HouseRepresentatives = 3 },
            new StateData { Name = "Nevada", Abbreviation = "NV", AreaSquareMiles = 110572, Population = 3104614, HouseRepresentatives = 4 },
            new StateData { Name = "New Hampshire", Abbreviation = "NH", AreaSquareMiles = 9349, Population = 1377529, HouseRepresentatives = 2 },
            new StateData { Name = "New Jersey", Abbreviation = "NJ", AreaSquareMiles = 8723, Population = 9288994, HouseRepresentatives = 12 },
            new StateData { Name = "New Mexico", Abbreviation = "NM", AreaSquareMiles = 121590, Population = 2117522, HouseRepresentatives = 3 },
            new StateData { Name = "New York", Abbreviation = "NY", AreaSquareMiles = 54555, Population = 20201249, HouseRepresentatives = 26 },
            new StateData { Name = "North Carolina", Abbreviation = "NC", AreaSquareMiles = 53819, Population = 10439388, HouseRepresentatives = 14 },
            new StateData { Name = "North Dakota", Abbreviation = "ND", AreaSquareMiles = 70698, Population = 779094, HouseRepresentatives = 1 },
            new StateData { Name = "Ohio", Abbreviation = "OH", AreaSquareMiles = 44826, Population = 11799448, HouseRepresentatives = 15 },
            new StateData { Name = "Oklahoma", Abbreviation = "OK", AreaSquareMiles = 69899, Population = 3959353, HouseRepresentatives = 5 },
            new StateData { Name = "Oregon", Abbreviation = "OR", AreaSquareMiles = 98379, Population = 4237256, HouseRepresentatives = 6 },
            new StateData { Name = "Pennsylvania", Abbreviation = "PA", AreaSquareMiles = 46054, Population = 13002700, HouseRepresentatives = 17 },
            new StateData { Name = "Rhode Island", Abbreviation = "RI", AreaSquareMiles = 1545, Population = 1097379, HouseRepresentatives = 2 },
            new StateData { Name = "South Carolina", Abbreviation = "SC", AreaSquareMiles = 32020, Population = 5118425, HouseRepresentatives = 7 },
            new StateData { Name = "South Dakota", Abbreviation = "SD", AreaSquareMiles = 77116, Population = 886667, HouseRepresentatives = 1 },
            new StateData { Name = "Tennessee", Abbreviation = "TN", AreaSquareMiles = 42144, Population = 6910840, HouseRepresentatives = 9 },
            new StateData { Name = "Texas", Abbreviation = "TX", AreaSquareMiles = 268596, Population = 29145505, HouseRepresentatives = 38 },
            new StateData { Name = "Utah", Abbreviation = "UT", AreaSquareMiles = 84897, Population = 3271616, HouseRepresentatives = 4 },
            new StateData { Name = "Vermont", Abbreviation = "VT", AreaSquareMiles = 9616, Population = 643077, HouseRepresentatives = 1 },
            new StateData { Name = "Virginia", Abbreviation = "VA", AreaSquareMiles = 42775, Population = 8631393, HouseRepresentatives = 11 },
            new StateData { Name = "Washington", Abbreviation = "WA", AreaSquareMiles = 71298, Population = 7614893, HouseRepresentatives = 10 },
            new StateData { Name = "West Virginia", Abbreviation = "WV", AreaSquareMiles = 24230, Population = 1793716, HouseRepresentatives = 2 },
            new StateData { Name = "Wisconsin", Abbreviation = "WI", AreaSquareMiles = 65496, Population = 5893718, HouseRepresentatives = 8 },
            new StateData { Name = "Wyoming", Abbreviation = "WY", AreaSquareMiles = 97813, Population = 576851, HouseRepresentatives = 1 }
        ];
    }
}
