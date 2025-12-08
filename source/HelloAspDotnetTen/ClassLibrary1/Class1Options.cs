namespace ClassLibrary1;

// Specific options for Class 1. 
// This prevents "God Object" settings where unrelated configs reside in one class.
public class Class1Options
{
    public const string SectionName = "Class1";
    public required string InjectedProperty1 { get; set; }
}
