namespace MH.Capstone.Domain.ApiContracts.Ninjas
{
    public record AnimalApiDto(string name, AnimalApiTaxonomyDto taxonomy, 
        AnimalApiLocations locations, AnimalApiCharacteristics characteristics)
    { }

    public record AnimalApiTaxonomyDto(string kingdom, string phylum,
        string taxClass, string order, string family, string genus,
        string scientificName)
    { }

    public record AnimalApiLocations(string[] locations)
    { }

    public record AnimalApiCharacteristics(
        string prey,
        string nameOfYoung,
        string groupBehavior,
        int estimatedPopulationSize,
        string biggestThreat,
        string mostDistinctiveFeature,
        string gestationPeriod,
        string habitat,
        string diet,
        int averageLitterSize,
        string lifestyle,
        string commonName,
        int numberOfSpecies,
        string location,
        string slogan,
        string group,
        string color,
        string skinType,
        string topSpeed,
        string lifespan,
        string weight,
        string height,
        string ageOfSexualMaturity,
        string ageOfWeaning)
    { }
}
