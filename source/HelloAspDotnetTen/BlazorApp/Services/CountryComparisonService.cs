using BlazorApp.Models;

namespace BlazorApp.Services;

/// <summary>
/// Service that manages country data and comparison game logic.
/// Only presents countries that have data for the selected question type.
/// </summary>
public class CountryComparisonService
{
    private readonly List<CountryData> _countries;
    private readonly List<CountryComparisonQuestion> _questions;
    private readonly Random _random = new();
    
    public CountryGameScore Score { get; } = new();
    
    public IReadOnlyList<CountryData> Countries => _countries;
    public IReadOnlyList<CountryComparisonQuestion> AvailableQuestions => _questions;
    
    public CountryComparisonService()
    {
        _countries = InitializeCountries();
        _questions = InitializeQuestions();
    }
    
    /// <summary>
    /// Gets two random countries that both have data for the specified question.
    /// </summary>
    public (CountryData Country1, CountryData Country2)? GetRandomCountryPair(CountryComparisonQuestion question)
    {
        var eligible = _countries.Where(c => question.HasDataFor(c)).ToList();
        if (eligible.Count < 2) return null;
        
        var idx1 = _random.Next(eligible.Count);
        int idx2;
        do { idx2 = _random.Next(eligible.Count); } 
        while (idx2 == idx1);
        
        return (eligible[idx1], eligible[idx2]);
    }
    
    /// <summary>
    /// Gets a random question from available questions.
    /// </summary>
    public CountryComparisonQuestion GetRandomQuestion()
    {
        return _questions[_random.Next(_questions.Count)];
    }
    
    /// <summary>
    /// Gets a random question that has at least 2 countries with data.
    /// </summary>
    public CountryComparisonQuestion GetRandomViableQuestion()
    {
        var viable = _questions.Where(q => _countries.Count(c => q.HasDataFor(c)) >= 2).ToList();
        return viable[_random.Next(viable.Count)];
    }
    
    /// <summary>
    /// Gets the count of countries with data for a specific question.
    /// </summary>
    public int GetCountryCountForQuestion(CountryComparisonQuestion question)
    {
        return _countries.Count(c => question.HasDataFor(c));
    }
    
    /// <summary>
    /// Determines which country has the higher value for the given question.
    /// </summary>
    public CountryData GetCorrectAnswer(CountryData c1, CountryData c2, CountryComparisonQuestion question)
    {
        var v1 = question.GetValue(c1) ?? 0;
        var v2 = question.GetValue(c2) ?? 0;
        return v1 >= v2 ? c1 : c2;
    }
    
    /// <summary>
    /// Checks the user's answer and records the result.
    /// </summary>
    public CountryComparisonResult CheckAnswer(CountryData c1, CountryData c2, 
        CountryComparisonQuestion question, CountryData userChoice)
    {
        var correct = GetCorrectAnswer(c1, c2, question);
        var result = new CountryComparisonResult
        {
            Country1 = c1,
            Country2 = c2,
            Question = question,
            CorrectAnswer = correct,
            UserChoice = userChoice
        };
        Score.RecordAnswer(result);
        return result;
    }
    
    private static List<CountryComparisonQuestion> InitializeQuestions()
    {
        return
        [
            new() {
                Id = "population",
                QuestionTemplate = "Which country has a larger population?",
                PropertyName = nameof(CountryData.Population),
                GetValue = c => c.Population,
                Unit = "people",
                FormatString = "{0:N0}"
            },
            new() {
                Id = "area",
                QuestionTemplate = "Which country is larger by area?",
                PropertyName = nameof(CountryData.AreaSquareKm),
                GetValue = c => c.AreaSquareKm,
                Unit = "km²",
                FormatString = "{0:N0} km²"
            },
            new() {
                Id = "gdp",
                QuestionTemplate = "Which country has a higher GDP?",
                PropertyName = nameof(CountryData.GdpMillionsUsd),
                GetValue = c => c.GdpMillionsUsd,
                Unit = "million USD",
                FormatString = "${0:N0}M"
            },
            new() {
                Id = "gdp_per_capita",
                QuestionTemplate = "Which country has a higher GDP per capita?",
                PropertyName = nameof(CountryData.GdpPerCapitaUsd),
                GetValue = c => c.GdpPerCapitaUsd,
                Unit = "USD",
                FormatString = "${0:N0}"
            },
            new() {
                Id = "gni_ppp",
                QuestionTemplate = "Which country has a higher GNI per capita (PPP)?",
                PropertyName = nameof(CountryData.GniPerCapitaPpp),
                GetValue = c => c.GniPerCapitaPpp,
                Unit = "int'l $",
                FormatString = "${0:N0}"
            },
            new() {
                Id = "density",
                QuestionTemplate = "Which country has higher population density?",
                PropertyName = nameof(CountryData.PopulationDensity),
                GetValue = c => c.PopulationDensity,
                Unit = "per km²",
                FormatString = "{0:N1}/km²"
            },
            new() {
                Id = "literacy",
                QuestionTemplate = "Which country has a higher literacy rate?",
                PropertyName = nameof(CountryData.LiteracyRate),
                GetValue = c => c.LiteracyRate,
                Unit = "%",
                FormatString = "{0:N1}%"
            },
            new() {
                Id = "hdi",
                QuestionTemplate = "Which country has a higher Human Development Index?",
                PropertyName = nameof(CountryData.Hdi),
                GetValue = c => c.Hdi,
                Unit = "HDI",
                FormatString = "{0:N3}"
            },
            new() {
                Id = "life_expectancy",
                QuestionTemplate = "Which country has higher life expectancy?",
                PropertyName = nameof(CountryData.LifeExpectancy),
                GetValue = c => c.LifeExpectancy,
                Unit = "years",
                FormatString = "{0:N1} years"
            }
        ];
    }
    
    private static List<CountryData> InitializeCountries()
    {
        // Data sources: World Bank, UN, IMF (2023/2024 estimates)
        // Null values indicate data not reliably available
        return
        [
            // A
            new() { Name = "Afghanistan", IsoCode2 = "AF", IsoCode3 = "AFG", FlagEmoji = "🇦🇫", Continent = "Asia", AreaSquareKm = 652230, Population = 42240000, GdpMillionsUsd = 14580, GdpPerCapitaUsd = 345, PopulationDensity = 64.8, LiteracyRate = 37.3, Hdi = 0.462, LifeExpectancy = 62.0 },
            new() { Name = "Albania", IsoCode2 = "AL", IsoCode3 = "ALB", FlagEmoji = "🇦🇱", Continent = "Europe", AreaSquareKm = 28748, Population = 2750000, GdpMillionsUsd = 22978, GdpPerCapitaUsd = 8358, GniPerCapitaPpp = 17620, PopulationDensity = 95.7, LiteracyRate = 98.4, Hdi = 0.789, LifeExpectancy = 76.5 },
            new() { Name = "Algeria", IsoCode2 = "DZ", IsoCode3 = "DZA", FlagEmoji = "🇩🇿", Continent = "Africa", AreaSquareKm = 2381741, Population = 45400000, GdpMillionsUsd = 239900, GdpPerCapitaUsd = 5284, GniPerCapitaPpp = 13310, PopulationDensity = 19.1, LiteracyRate = 81.4, Hdi = 0.745, LifeExpectancy = 76.4 },
            new() { Name = "Andorra", IsoCode2 = "AD", IsoCode3 = "AND", FlagEmoji = "🇦🇩", Continent = "Europe", AreaSquareKm = 468, Population = 80000, GdpMillionsUsd = 3352, GdpPerCapitaUsd = 41900, PopulationDensity = 170.9, LiteracyRate = 100.0, Hdi = 0.884, LifeExpectancy = 83.0 },
            new() { Name = "Angola", IsoCode2 = "AO", IsoCode3 = "AGO", FlagEmoji = "🇦🇴", Continent = "Africa", AreaSquareKm = 1246700, Population = 36000000, GdpMillionsUsd = 92124, GdpPerCapitaUsd = 2560, GniPerCapitaPpp = 7250, PopulationDensity = 28.9, LiteracyRate = 72.0, Hdi = 0.595, LifeExpectancy = 62.0 },
            new() { Name = "Antigua and Barbuda", IsoCode2 = "AG", IsoCode3 = "ATG", FlagEmoji = "🇦🇬", Continent = "North America", AreaSquareKm = 443, Population = 94000, GdpMillionsUsd = 1868, GdpPerCapitaUsd = 19872, GniPerCapitaPpp = 23490, PopulationDensity = 212.2, LiteracyRate = 99.0, Hdi = 0.820, LifeExpectancy = 78.0 },
            new() { Name = "Argentina", IsoCode2 = "AR", IsoCode3 = "ARG", FlagEmoji = "🇦🇷", Continent = "South America", AreaSquareKm = 2780400, Population = 46300000, GdpMillionsUsd = 641131, GdpPerCapitaUsd = 13846, GniPerCapitaPpp = 26390, PopulationDensity = 16.7, LiteracyRate = 99.0, Hdi = 0.849, LifeExpectancy = 76.6 },
            new() { Name = "Armenia", IsoCode2 = "AM", IsoCode3 = "ARM", FlagEmoji = "🇦🇲", Continent = "Asia", AreaSquareKm = 29743, Population = 2780000, GdpMillionsUsd = 24212, GdpPerCapitaUsd = 8712, GniPerCapitaPpp = 18480, PopulationDensity = 93.5, LiteracyRate = 99.8, Hdi = 0.786, LifeExpectancy = 72.0 },
            new() { Name = "Australia", IsoCode2 = "AU", IsoCode3 = "AUS", FlagEmoji = "🇦🇺", Continent = "Oceania", AreaSquareKm = 7692024, Population = 26500000, GdpMillionsUsd = 1687713, GdpPerCapitaUsd = 63688, GniPerCapitaPpp = 59170, PopulationDensity = 3.4, LiteracyRate = 99.0, Hdi = 0.946, LifeExpectancy = 84.5 },
            new() { Name = "Austria", IsoCode2 = "AT", IsoCode3 = "AUT", FlagEmoji = "🇦🇹", Continent = "Europe", AreaSquareKm = 83871, Population = 9100000, GdpMillionsUsd = 515199, GdpPerCapitaUsd = 56593, GniPerCapitaPpp = 66640, PopulationDensity = 108.5, LiteracyRate = 99.0, Hdi = 0.926, LifeExpectancy = 82.0 },
            new() { Name = "Azerbaijan", IsoCode2 = "AZ", IsoCode3 = "AZE", FlagEmoji = "🇦🇿", Continent = "Asia", AreaSquareKm = 86600, Population = 10200000, GdpMillionsUsd = 72356, GdpPerCapitaUsd = 7094, GniPerCapitaPpp = 17090, PopulationDensity = 117.8, LiteracyRate = 99.8, Hdi = 0.760, LifeExpectancy = 69.4 },
            
            // B
            new() { Name = "Bahamas", IsoCode2 = "BS", IsoCode3 = "BHS", FlagEmoji = "🇧🇸", Continent = "North America", AreaSquareKm = 13943, Population = 410000, GdpMillionsUsd = 14004, GdpPerCapitaUsd = 34156, GniPerCapitaPpp = 37250, PopulationDensity = 29.4, LiteracyRate = 95.6, Hdi = 0.820, LifeExpectancy = 71.6 },
            new() { Name = "Bahrain", IsoCode2 = "BH", IsoCode3 = "BHR", FlagEmoji = "🇧🇭", Continent = "Asia", AreaSquareKm = 765, Population = 1500000, GdpMillionsUsd = 44169, GdpPerCapitaUsd = 29446, GniPerCapitaPpp = 54810, PopulationDensity = 1960.8, LiteracyRate = 97.5, Hdi = 0.888, LifeExpectancy = 78.8 },
            new() { Name = "Bangladesh", IsoCode2 = "BD", IsoCode3 = "BGD", FlagEmoji = "🇧🇩", Continent = "Asia", AreaSquareKm = 147570, Population = 173000000, GdpMillionsUsd = 437415, GdpPerCapitaUsd = 2529, GniPerCapitaPpp = 7130, PopulationDensity = 1172.6, LiteracyRate = 74.9, Hdi = 0.670, LifeExpectancy = 72.4 },
            new() { Name = "Barbados", IsoCode2 = "BB", IsoCode3 = "BRB", FlagEmoji = "🇧🇧", Continent = "North America", AreaSquareKm = 430, Population = 282000, GdpMillionsUsd = 6112, GdpPerCapitaUsd = 21674, GniPerCapitaPpp = 17560, PopulationDensity = 655.8, LiteracyRate = 99.6, Hdi = 0.809, LifeExpectancy = 77.6 },
            new() { Name = "Belarus", IsoCode2 = "BY", IsoCode3 = "BLR", FlagEmoji = "🇧🇾", Continent = "Europe", AreaSquareKm = 207600, Population = 9200000, GdpMillionsUsd = 72881, GdpPerCapitaUsd = 7922, GniPerCapitaPpp = 22020, PopulationDensity = 44.3, LiteracyRate = 99.8, Hdi = 0.801, LifeExpectancy = 72.4 },
            new() { Name = "Belgium", IsoCode2 = "BE", IsoCode3 = "BEL", FlagEmoji = "🇧🇪", Continent = "Europe", AreaSquareKm = 30528, Population = 11700000, GdpMillionsUsd = 627511, GdpPerCapitaUsd = 53642, GniPerCapitaPpp = 64030, PopulationDensity = 383.3, LiteracyRate = 99.0, Hdi = 0.942, LifeExpectancy = 82.0 },
            new() { Name = "Belize", IsoCode2 = "BZ", IsoCode3 = "BLZ", FlagEmoji = "🇧🇿", Continent = "North America", AreaSquareKm = 22966, Population = 410000, GdpMillionsUsd = 3218, GdpPerCapitaUsd = 7849, GniPerCapitaPpp = 11220, PopulationDensity = 17.9, LiteracyRate = 82.7, Hdi = 0.700, LifeExpectancy = 70.5 },
            new() { Name = "Benin", IsoCode2 = "BJ", IsoCode3 = "BEN", FlagEmoji = "🇧🇯", Continent = "Africa", AreaSquareKm = 112622, Population = 13700000, GdpMillionsUsd = 19234, GdpPerCapitaUsd = 1404, GniPerCapitaPpp = 4030, PopulationDensity = 121.7, LiteracyRate = 45.8, Hdi = 0.504, LifeExpectancy = 60.0 },
            new() { Name = "Bhutan", IsoCode2 = "BT", IsoCode3 = "BTN", FlagEmoji = "🇧🇹", Continent = "Asia", AreaSquareKm = 38394, Population = 780000, GdpMillionsUsd = 2898, GdpPerCapitaUsd = 3715, GniPerCapitaPpp = 12640, PopulationDensity = 20.3, LiteracyRate = 66.6, Hdi = 0.666, LifeExpectancy = 72.1 },
            new() { Name = "Bolivia", IsoCode2 = "BO", IsoCode3 = "BOL", FlagEmoji = "🇧🇴", Continent = "South America", AreaSquareKm = 1098581, Population = 12200000, GdpMillionsUsd = 45464, GdpPerCapitaUsd = 3727, GniPerCapitaPpp = 9040, PopulationDensity = 11.1, LiteracyRate = 94.5, Hdi = 0.698, LifeExpectancy = 63.6 },
            new() { Name = "Bosnia and Herzegovina", IsoCode2 = "BA", IsoCode3 = "BIH", FlagEmoji = "🇧🇦", Continent = "Europe", AreaSquareKm = 51197, Population = 3210000, GdpMillionsUsd = 27034, GdpPerCapitaUsd = 8422, GniPerCapitaPpp = 19270, PopulationDensity = 62.7, LiteracyRate = 98.5, Hdi = 0.779, LifeExpectancy = 75.3 },
            new() { Name = "Botswana", IsoCode2 = "BW", IsoCode3 = "BWA", FlagEmoji = "🇧🇼", Continent = "Africa", AreaSquareKm = 581730, Population = 2600000, GdpMillionsUsd = 19396, GdpPerCapitaUsd = 7460, GniPerCapitaPpp = 18600, PopulationDensity = 4.5, LiteracyRate = 88.9, Hdi = 0.708, LifeExpectancy = 61.1 },
            new() { Name = "Brazil", IsoCode2 = "BR", IsoCode3 = "BRA", FlagEmoji = "🇧🇷", Continent = "South America", AreaSquareKm = 8515767, Population = 216400000, GdpMillionsUsd = 2173669, GdpPerCapitaUsd = 10044, GniPerCapitaPpp = 17660, PopulationDensity = 25.4, LiteracyRate = 93.2, Hdi = 0.760, LifeExpectancy = 72.8 },
            new() { Name = "Brunei", IsoCode2 = "BN", IsoCode3 = "BRN", FlagEmoji = "🇧🇳", Continent = "Asia", AreaSquareKm = 5765, Population = 450000, GdpMillionsUsd = 15128, GdpPerCapitaUsd = 33618, GniPerCapitaPpp = 71620, PopulationDensity = 78.1, LiteracyRate = 97.2, Hdi = 0.907, LifeExpectancy = 74.6 },
            new() { Name = "Bulgaria", IsoCode2 = "BG", IsoCode3 = "BGR", FlagEmoji = "🇧🇬", Continent = "Europe", AreaSquareKm = 110879, Population = 6500000, GdpMillionsUsd = 100635, GdpPerCapitaUsd = 15483, GniPerCapitaPpp = 29860, PopulationDensity = 58.6, LiteracyRate = 98.4, Hdi = 0.799, LifeExpectancy = 71.8 },
            new() { Name = "Burkina Faso", IsoCode2 = "BF", IsoCode3 = "BFA", FlagEmoji = "🇧🇫", Continent = "Africa", AreaSquareKm = 274200, Population = 23000000, GdpMillionsUsd = 20327, GdpPerCapitaUsd = 884, GniPerCapitaPpp = 2310, PopulationDensity = 83.9, LiteracyRate = 34.5, Hdi = 0.438, LifeExpectancy = 59.3 },
            new() { Name = "Burundi", IsoCode2 = "BI", IsoCode3 = "BDI", FlagEmoji = "🇧🇮", Continent = "Africa", AreaSquareKm = 27834, Population = 13200000, GdpMillionsUsd = 2779, GdpPerCapitaUsd = 211, GniPerCapitaPpp = 780, PopulationDensity = 474.3, LiteracyRate = 74.7, Hdi = 0.426, LifeExpectancy = 61.7 },
            
            // C
            new() { Name = "Cabo Verde", IsoCode2 = "CV", IsoCode3 = "CPV", FlagEmoji = "🇨🇻", Continent = "Africa", AreaSquareKm = 4033, Population = 600000, GdpMillionsUsd = 2290, GdpPerCapitaUsd = 3817, GniPerCapitaPpp = 9420, PopulationDensity = 148.8, LiteracyRate = 90.8, Hdi = 0.662, LifeExpectancy = 74.1 },
            new() { Name = "Cambodia", IsoCode2 = "KH", IsoCode3 = "KHM", FlagEmoji = "🇰🇭", Continent = "Asia", AreaSquareKm = 181035, Population = 17000000, GdpMillionsUsd = 31772, GdpPerCapitaUsd = 1869, GniPerCapitaPpp = 5080, PopulationDensity = 93.9, LiteracyRate = 83.9, Hdi = 0.600, LifeExpectancy = 70.0 },
            new() { Name = "Cameroon", IsoCode2 = "CM", IsoCode3 = "CMR", FlagEmoji = "🇨🇲", Continent = "Africa", AreaSquareKm = 475442, Population = 28600000, GdpMillionsUsd = 48455, GdpPerCapitaUsd = 1694, GniPerCapitaPpp = 4220, PopulationDensity = 60.2, LiteracyRate = 77.1, Hdi = 0.587, LifeExpectancy = 61.0 },
            new() { Name = "Canada", IsoCode2 = "CA", IsoCode3 = "CAN", FlagEmoji = "🇨🇦", Continent = "North America", AreaSquareKm = 9984670, Population = 40100000, GdpMillionsUsd = 2117805, GdpPerCapitaUsd = 52819, GniPerCapitaPpp = 58310, PopulationDensity = 4.0, LiteracyRate = 99.0, Hdi = 0.935, LifeExpectancy = 82.7 },
            new() { Name = "Central African Republic", IsoCode2 = "CF", IsoCode3 = "CAF", FlagEmoji = "🇨🇫", Continent = "Africa", AreaSquareKm = 622984, Population = 5500000, GdpMillionsUsd = 2553, GdpPerCapitaUsd = 464, GniPerCapitaPpp = 1130, PopulationDensity = 8.8, LiteracyRate = 37.5, Hdi = 0.387, LifeExpectancy = 54.0 },
            new() { Name = "Chad", IsoCode2 = "TD", IsoCode3 = "TCD", FlagEmoji = "🇹🇩", Continent = "Africa", AreaSquareKm = 1284000, Population = 18300000, GdpMillionsUsd = 12698, GdpPerCapitaUsd = 694, GniPerCapitaPpp = 1640, PopulationDensity = 14.3, LiteracyRate = 27.3, Hdi = 0.394, LifeExpectancy = 52.5 },
            new() { Name = "Chile", IsoCode2 = "CL", IsoCode3 = "CHL", FlagEmoji = "🇨🇱", Continent = "South America", AreaSquareKm = 756102, Population = 19500000, GdpMillionsUsd = 335533, GdpPerCapitaUsd = 17206, GniPerCapitaPpp = 28510, PopulationDensity = 25.8, LiteracyRate = 97.0, Hdi = 0.860, LifeExpectancy = 78.9 },
            new() { Name = "China", IsoCode2 = "CN", IsoCode3 = "CHN", FlagEmoji = "🇨🇳", Continent = "Asia", AreaSquareKm = 9596960, Population = 1410000000, GdpMillionsUsd = 17794782, GdpPerCapitaUsd = 12614, GniPerCapitaPpp = 23930, PopulationDensity = 146.9, LiteracyRate = 97.3, Hdi = 0.788, LifeExpectancy = 78.6 },
            new() { Name = "Colombia", IsoCode2 = "CO", IsoCode3 = "COL", FlagEmoji = "🇨🇴", Continent = "South America", AreaSquareKm = 1138910, Population = 52000000, GdpMillionsUsd = 363835, GdpPerCapitaUsd = 6997, GniPerCapitaPpp = 18180, PopulationDensity = 45.7, LiteracyRate = 95.6, Hdi = 0.758, LifeExpectancy = 72.8 },
            new() { Name = "Comoros", IsoCode2 = "KM", IsoCode3 = "COM", FlagEmoji = "🇰🇲", Continent = "Africa", AreaSquareKm = 2235, Population = 840000, GdpMillionsUsd = 1296, GdpPerCapitaUsd = 1543, GniPerCapitaPpp = 3620, PopulationDensity = 375.8, LiteracyRate = 62.0, Hdi = 0.596, LifeExpectancy = 63.4 },
            new() { Name = "DR Congo", IsoCode2 = "CD", IsoCode3 = "COD", FlagEmoji = "🇨🇩", Continent = "Africa", AreaSquareKm = 2344858, Population = 102300000, GdpMillionsUsd = 66380, GdpPerCapitaUsd = 649, GniPerCapitaPpp = 1430, PopulationDensity = 43.6, LiteracyRate = 80.0, Hdi = 0.479, LifeExpectancy = 60.7 },
            new() { Name = "Republic of the Congo", IsoCode2 = "CG", IsoCode3 = "COG", FlagEmoji = "🇨🇬", Continent = "Africa", AreaSquareKm = 342000, Population = 6100000, GdpMillionsUsd = 13366, GdpPerCapitaUsd = 2191, GniPerCapitaPpp = 4080, PopulationDensity = 17.8, LiteracyRate = 80.3, Hdi = 0.593, LifeExpectancy = 63.5 },
            new() { Name = "Costa Rica", IsoCode2 = "CR", IsoCode3 = "CRI", FlagEmoji = "🇨🇷", Continent = "North America", AreaSquareKm = 51100, Population = 5200000, GdpMillionsUsd = 85551, GdpPerCapitaUsd = 16452, GniPerCapitaPpp = 24060, PopulationDensity = 101.8, LiteracyRate = 98.0, Hdi = 0.806, LifeExpectancy = 77.0 },
            new() { Name = "Côte d'Ivoire", IsoCode2 = "CI", IsoCode3 = "CIV", FlagEmoji = "🇨🇮", Continent = "Africa", AreaSquareKm = 322463, Population = 28900000, GdpMillionsUsd = 78788, GdpPerCapitaUsd = 2726, GniPerCapitaPpp = 6450, PopulationDensity = 89.6, LiteracyRate = 53.1, Hdi = 0.534, LifeExpectancy = 58.6 },
            new() { Name = "Croatia", IsoCode2 = "HR", IsoCode3 = "HRV", FlagEmoji = "🇭🇷", Continent = "Europe", AreaSquareKm = 56594, Population = 3900000, GdpMillionsUsd = 82689, GdpPerCapitaUsd = 21202, GniPerCapitaPpp = 38050, PopulationDensity = 68.9, LiteracyRate = 99.3, Hdi = 0.878, LifeExpectancy = 76.6 },
            new() { Name = "Cuba", IsoCode2 = "CU", IsoCode3 = "CUB", FlagEmoji = "🇨🇺", Continent = "North America", AreaSquareKm = 109884, Population = 11100000, GdpMillionsUsd = 107352, GdpPerCapitaUsd = 9673, PopulationDensity = 101.0, LiteracyRate = 99.7, Hdi = 0.764, LifeExpectancy = 73.7 },
            new() { Name = "Cyprus", IsoCode2 = "CY", IsoCode3 = "CYP", FlagEmoji = "🇨🇾", Continent = "Europe", AreaSquareKm = 9251, Population = 1260000, GdpMillionsUsd = 32229, GdpPerCapitaUsd = 25578, GniPerCapitaPpp = 49460, PopulationDensity = 136.2, LiteracyRate = 99.1, Hdi = 0.907, LifeExpectancy = 81.2 },
            new() { Name = "Czech Republic", IsoCode2 = "CZ", IsoCode3 = "CZE", FlagEmoji = "🇨🇿", Continent = "Europe", AreaSquareKm = 78867, Population = 10500000, GdpMillionsUsd = 330483, GdpPerCapitaUsd = 31475, GniPerCapitaPpp = 49600, PopulationDensity = 133.1, LiteracyRate = 99.0, Hdi = 0.895, LifeExpectancy = 77.7 },
            
            // D
            new() { Name = "Denmark", IsoCode2 = "DK", IsoCode3 = "DNK", FlagEmoji = "🇩🇰", Continent = "Europe", AreaSquareKm = 43094, Population = 5900000, GdpMillionsUsd = 404198, GdpPerCapitaUsd = 68513, GniPerCapitaPpp = 73790, PopulationDensity = 136.9, LiteracyRate = 99.0, Hdi = 0.952, LifeExpectancy = 81.4 },
            new() { Name = "Djibouti", IsoCode2 = "DJ", IsoCode3 = "DJI", FlagEmoji = "🇩🇯", Continent = "Africa", AreaSquareKm = 23200, Population = 1100000, GdpMillionsUsd = 3668, GdpPerCapitaUsd = 3335, GniPerCapitaPpp = 6760, PopulationDensity = 47.4, LiteracyRate = 53.0, Hdi = 0.521, LifeExpectancy = 62.3 },
            new() { Name = "Dominica", IsoCode2 = "DM", IsoCode3 = "DMA", FlagEmoji = "🇩🇲", Continent = "North America", AreaSquareKm = 751, Population = 73000, GdpMillionsUsd = 654, GdpPerCapitaUsd = 8959, GniPerCapitaPpp = 14290, PopulationDensity = 97.2, LiteracyRate = 94.0, Hdi = 0.720, LifeExpectancy = 78.0 },
            new() { Name = "Dominican Republic", IsoCode2 = "DO", IsoCode3 = "DOM", FlagEmoji = "🇩🇴", Continent = "North America", AreaSquareKm = 48671, Population = 11300000, GdpMillionsUsd = 113641, GdpPerCapitaUsd = 10057, GniPerCapitaPpp = 23500, PopulationDensity = 232.2, LiteracyRate = 95.0, Hdi = 0.766, LifeExpectancy = 72.6 },
            
            // E
            new() { Name = "Ecuador", IsoCode2 = "EC", IsoCode3 = "ECU", FlagEmoji = "🇪🇨", Continent = "South America", AreaSquareKm = 283561, Population = 18000000, GdpMillionsUsd = 118845, GdpPerCapitaUsd = 6603, GniPerCapitaPpp = 12930, PopulationDensity = 63.5, LiteracyRate = 94.5, Hdi = 0.765, LifeExpectancy = 74.3 },
            new() { Name = "Egypt", IsoCode2 = "EG", IsoCode3 = "EGY", FlagEmoji = "🇪🇬", Continent = "Africa", AreaSquareKm = 1001450, Population = 105000000, GdpMillionsUsd = 387086, GdpPerCapitaUsd = 3686, GniPerCapitaPpp = 16450, PopulationDensity = 104.9, LiteracyRate = 73.1, Hdi = 0.728, LifeExpectancy = 70.2 },
            new() { Name = "El Salvador", IsoCode2 = "SV", IsoCode3 = "SLV", FlagEmoji = "🇸🇻", Continent = "North America", AreaSquareKm = 21041, Population = 6300000, GdpMillionsUsd = 33001, GdpPerCapitaUsd = 5238, GniPerCapitaPpp = 10330, PopulationDensity = 299.5, LiteracyRate = 89.7, Hdi = 0.674, LifeExpectancy = 70.7 },
            new() { Name = "Equatorial Guinea", IsoCode2 = "GQ", IsoCode3 = "GNQ", FlagEmoji = "🇬🇶", Continent = "Africa", AreaSquareKm = 28051, Population = 1700000, GdpMillionsUsd = 12269, GdpPerCapitaUsd = 7217, GniPerCapitaPpp = 17270, PopulationDensity = 60.6, LiteracyRate = 95.3, Hdi = 0.596, LifeExpectancy = 60.6 },
            new() { Name = "Eritrea", IsoCode2 = "ER", IsoCode3 = "ERI", FlagEmoji = "🇪🇷", Continent = "Africa", AreaSquareKm = 117600, Population = 3700000, GdpMillionsUsd = 2065, GdpPerCapitaUsd = 558, PopulationDensity = 31.5, LiteracyRate = 76.6, Hdi = 0.492, LifeExpectancy = 67.5 },
            new() { Name = "Estonia", IsoCode2 = "EE", IsoCode3 = "EST", FlagEmoji = "🇪🇪", Continent = "Europe", AreaSquareKm = 45228, Population = 1370000, GdpMillionsUsd = 41551, GdpPerCapitaUsd = 30329, GniPerCapitaPpp = 45400, PopulationDensity = 30.3, LiteracyRate = 99.9, Hdi = 0.899, LifeExpectancy = 77.1 },
            new() { Name = "Eswatini", IsoCode2 = "SZ", IsoCode3 = "SWZ", FlagEmoji = "🇸🇿", Continent = "Africa", AreaSquareKm = 17364, Population = 1200000, GdpMillionsUsd = 4854, GdpPerCapitaUsd = 4045, GniPerCapitaPpp = 9310, PopulationDensity = 69.1, LiteracyRate = 88.4, Hdi = 0.597, LifeExpectancy = 57.1 },
            new() { Name = "Ethiopia", IsoCode2 = "ET", IsoCode3 = "ETH", FlagEmoji = "🇪🇹", Continent = "Africa", AreaSquareKm = 1104300, Population = 126500000, GdpMillionsUsd = 163698, GdpPerCapitaUsd = 1294, GniPerCapitaPpp = 3070, PopulationDensity = 114.6, LiteracyRate = 51.8, Hdi = 0.492, LifeExpectancy = 65.0 },
            
            // F
            new() { Name = "Fiji", IsoCode2 = "FJ", IsoCode3 = "FJI", FlagEmoji = "🇫🇯", Continent = "Oceania", AreaSquareKm = 18274, Population = 930000, GdpMillionsUsd = 5314, GdpPerCapitaUsd = 5714, GniPerCapitaPpp = 14570, PopulationDensity = 50.9, LiteracyRate = 99.1, Hdi = 0.729, LifeExpectancy = 67.4 },
            new() { Name = "Finland", IsoCode2 = "FI", IsoCode3 = "FIN", FlagEmoji = "🇫🇮", Continent = "Europe", AreaSquareKm = 338145, Population = 5500000, GdpMillionsUsd = 305689, GdpPerCapitaUsd = 55580, GniPerCapitaPpp = 57500, PopulationDensity = 16.3, LiteracyRate = 99.0, Hdi = 0.942, LifeExpectancy = 82.0 },
            new() { Name = "France", IsoCode2 = "FR", IsoCode3 = "FRA", FlagEmoji = "🇫🇷", Continent = "Europe", AreaSquareKm = 643801, Population = 68000000, GdpMillionsUsd = 3030901, GdpPerCapitaUsd = 44573, GniPerCapitaPpp = 55080, PopulationDensity = 105.6, LiteracyRate = 99.0, Hdi = 0.910, LifeExpectancy = 82.5 },
            
            // G
            new() { Name = "Gabon", IsoCode2 = "GA", IsoCode3 = "GAB", FlagEmoji = "🇬🇦", Continent = "Africa", AreaSquareKm = 267668, Population = 2400000, GdpMillionsUsd = 19316, GdpPerCapitaUsd = 8048, GniPerCapitaPpp = 17430, PopulationDensity = 9.0, LiteracyRate = 85.0, Hdi = 0.706, LifeExpectancy = 66.5 },
            new() { Name = "Gambia", IsoCode2 = "GM", IsoCode3 = "GMB", FlagEmoji = "🇬🇲", Continent = "Africa", AreaSquareKm = 11295, Population = 2700000, GdpMillionsUsd = 2187, GdpPerCapitaUsd = 810, GniPerCapitaPpp = 2350, PopulationDensity = 239.0, LiteracyRate = 58.1, Hdi = 0.500, LifeExpectancy = 62.6 },
            new() { Name = "Georgia", IsoCode2 = "GE", IsoCode3 = "GEO", FlagEmoji = "🇬🇪", Continent = "Asia", AreaSquareKm = 69700, Population = 3700000, GdpMillionsUsd = 30536, GdpPerCapitaUsd = 8253, GniPerCapitaPpp = 20590, PopulationDensity = 53.1, LiteracyRate = 99.6, Hdi = 0.814, LifeExpectancy = 71.7 },
            new() { Name = "Germany", IsoCode2 = "DE", IsoCode3 = "DEU", FlagEmoji = "🇩🇪", Continent = "Europe", AreaSquareKm = 357022, Population = 84400000, GdpMillionsUsd = 4430758, GdpPerCapitaUsd = 52485, GniPerCapitaPpp = 64770, PopulationDensity = 236.4, LiteracyRate = 99.0, Hdi = 0.950, LifeExpectancy = 80.6 },
            new() { Name = "Ghana", IsoCode2 = "GH", IsoCode3 = "GHA", FlagEmoji = "🇬🇭", Continent = "Africa", AreaSquareKm = 238533, Population = 34100000, GdpMillionsUsd = 76370, GdpPerCapitaUsd = 2240, GniPerCapitaPpp = 5870, PopulationDensity = 143.0, LiteracyRate = 79.0, Hdi = 0.602, LifeExpectancy = 63.8 },
            new() { Name = "Greece", IsoCode2 = "GR", IsoCode3 = "GRC", FlagEmoji = "🇬🇷", Continent = "Europe", AreaSquareKm = 131957, Population = 10400000, GdpMillionsUsd = 238206, GdpPerCapitaUsd = 22905, GniPerCapitaPpp = 37050, PopulationDensity = 78.8, LiteracyRate = 97.9, Hdi = 0.893, LifeExpectancy = 80.1 },
            new() { Name = "Grenada", IsoCode2 = "GD", IsoCode3 = "GRD", FlagEmoji = "🇬🇩", Continent = "North America", AreaSquareKm = 344, Population = 126000, GdpMillionsUsd = 1326, GdpPerCapitaUsd = 10524, GniPerCapitaPpp = 19640, PopulationDensity = 366.3, LiteracyRate = 98.6, Hdi = 0.795, LifeExpectancy = 74.9 },
            new() { Name = "Guatemala", IsoCode2 = "GT", IsoCode3 = "GTM", FlagEmoji = "🇬🇹", Continent = "North America", AreaSquareKm = 108889, Population = 17600000, GdpMillionsUsd = 102050, GdpPerCapitaUsd = 5798, GniPerCapitaPpp = 10960, PopulationDensity = 161.6, LiteracyRate = 83.3, Hdi = 0.629, LifeExpectancy = 69.2 },
            new() { Name = "Guinea", IsoCode2 = "GN", IsoCode3 = "GIN", FlagEmoji = "🇬🇳", Continent = "Africa", AreaSquareKm = 245857, Population = 14200000, GdpMillionsUsd = 20310, GdpPerCapitaUsd = 1430, GniPerCapitaPpp = 3100, PopulationDensity = 57.8, LiteracyRate = 45.3, Hdi = 0.465, LifeExpectancy = 58.9 },
            new() { Name = "Guinea-Bissau", IsoCode2 = "GW", IsoCode3 = "GNB", FlagEmoji = "🇬🇼", Continent = "Africa", AreaSquareKm = 36125, Population = 2100000, GdpMillionsUsd = 1639, GdpPerCapitaUsd = 780, GniPerCapitaPpp = 2050, PopulationDensity = 58.1, LiteracyRate = 52.9, Hdi = 0.483, LifeExpectancy = 60.0 },
            new() { Name = "Guyana", IsoCode2 = "GY", IsoCode3 = "GUY", FlagEmoji = "🇬🇾", Continent = "South America", AreaSquareKm = 214969, Population = 810000, GdpMillionsUsd = 16322, GdpPerCapitaUsd = 20151, GniPerCapitaPpp = 34220, PopulationDensity = 3.8, LiteracyRate = 88.5, Hdi = 0.742, LifeExpectancy = 65.7 },
            
            // H
            new() { Name = "Haiti", IsoCode2 = "HT", IsoCode3 = "HTI", FlagEmoji = "🇭🇹", Continent = "North America", AreaSquareKm = 27750, Population = 11700000, GdpMillionsUsd = 20168, GdpPerCapitaUsd = 1723, GniPerCapitaPpp = 3150, PopulationDensity = 421.6, LiteracyRate = 61.7, Hdi = 0.552, LifeExpectancy = 63.2 },
            new() { Name = "Honduras", IsoCode2 = "HN", IsoCode3 = "HND", FlagEmoji = "🇭🇳", Continent = "North America", AreaSquareKm = 112492, Population = 10400000, GdpMillionsUsd = 34401, GdpPerCapitaUsd = 3308, GniPerCapitaPpp = 6650, PopulationDensity = 92.5, LiteracyRate = 88.5, Hdi = 0.624, LifeExpectancy = 70.1 },
            new() { Name = "Hungary", IsoCode2 = "HU", IsoCode3 = "HUN", FlagEmoji = "🇭🇺", Continent = "Europe", AreaSquareKm = 93028, Population = 9600000, GdpMillionsUsd = 212389, GdpPerCapitaUsd = 22124, GniPerCapitaPpp = 40200, PopulationDensity = 103.2, LiteracyRate = 99.1, Hdi = 0.846, LifeExpectancy = 74.5 },
            
            // I
            new() { Name = "Iceland", IsoCode2 = "IS", IsoCode3 = "ISL", FlagEmoji = "🇮🇸", Continent = "Europe", AreaSquareKm = 103000, Population = 380000, GdpMillionsUsd = 31020, GdpPerCapitaUsd = 81632, GniPerCapitaPpp = 69050, PopulationDensity = 3.7, LiteracyRate = 99.0, Hdi = 0.972, LifeExpectancy = 83.0 },
            new() { Name = "India", IsoCode2 = "IN", IsoCode3 = "IND", FlagEmoji = "🇮🇳", Continent = "Asia", AreaSquareKm = 3287263, Population = 1428600000, GdpMillionsUsd = 3549918, GdpPerCapitaUsd = 2485, GniPerCapitaPpp = 8610, PopulationDensity = 434.6, LiteracyRate = 77.7, Hdi = 0.644, LifeExpectancy = 67.2 },
            new() { Name = "Indonesia", IsoCode2 = "ID", IsoCode3 = "IDN", FlagEmoji = "🇮🇩", Continent = "Asia", AreaSquareKm = 1904569, Population = 277500000, GdpMillionsUsd = 1417387, GdpPerCapitaUsd = 5108, GniPerCapitaPpp = 15190, PopulationDensity = 145.7, LiteracyRate = 96.0, Hdi = 0.713, LifeExpectancy = 68.6 },
            new() { Name = "Iran", IsoCode2 = "IR", IsoCode3 = "IRN", FlagEmoji = "🇮🇷", Continent = "Asia", AreaSquareKm = 1648195, Population = 89200000, GdpMillionsUsd = 413493, GdpPerCapitaUsd = 4635, GniPerCapitaPpp = 16740, PopulationDensity = 54.1, LiteracyRate = 88.7, Hdi = 0.780, LifeExpectancy = 74.0 },
            new() { Name = "Iraq", IsoCode2 = "IQ", IsoCode3 = "IRQ", FlagEmoji = "🇮🇶", Continent = "Asia", AreaSquareKm = 438317, Population = 44500000, GdpMillionsUsd = 270025, GdpPerCapitaUsd = 6071, GniPerCapitaPpp = 12340, PopulationDensity = 101.5, LiteracyRate = 85.6, Hdi = 0.673, LifeExpectancy = 70.4 },
            new() { Name = "Ireland", IsoCode2 = "IE", IsoCode3 = "IRL", FlagEmoji = "🇮🇪", Continent = "Europe", AreaSquareKm = 70273, Population = 5100000, GdpMillionsUsd = 533682, GdpPerCapitaUsd = 104604, GniPerCapitaPpp = 83140, PopulationDensity = 72.6, LiteracyRate = 99.0, Hdi = 0.950, LifeExpectancy = 82.0 },
            new() { Name = "Israel", IsoCode2 = "IL", IsoCode3 = "ISR", FlagEmoji = "🇮🇱", Continent = "Asia", AreaSquareKm = 20770, Population = 9800000, GdpMillionsUsd = 521688, GdpPerCapitaUsd = 53233, GniPerCapitaPpp = 48520, PopulationDensity = 471.8, LiteracyRate = 97.8, Hdi = 0.915, LifeExpectancy = 82.0 },
            new() { Name = "Italy", IsoCode2 = "IT", IsoCode3 = "ITA", FlagEmoji = "🇮🇹", Continent = "Europe", AreaSquareKm = 301340, Population = 58900000, GdpMillionsUsd = 2254851, GdpPerCapitaUsd = 38285, GniPerCapitaPpp = 53470, PopulationDensity = 195.5, LiteracyRate = 99.2, Hdi = 0.906, LifeExpectancy = 83.0 },
            
            // J
            new() { Name = "Jamaica", IsoCode2 = "JM", IsoCode3 = "JAM", FlagEmoji = "🇯🇲", Continent = "North America", AreaSquareKm = 10991, Population = 2800000, GdpMillionsUsd = 18777, GdpPerCapitaUsd = 6706, GniPerCapitaPpp = 11600, PopulationDensity = 254.7, LiteracyRate = 88.7, Hdi = 0.706, LifeExpectancy = 70.5 },
            new() { Name = "Japan", IsoCode2 = "JP", IsoCode3 = "JPN", FlagEmoji = "🇯🇵", Continent = "Asia", AreaSquareKm = 377930, Population = 123300000, Gdp