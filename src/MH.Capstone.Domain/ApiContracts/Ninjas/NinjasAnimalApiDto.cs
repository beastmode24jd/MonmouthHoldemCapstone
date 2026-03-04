namespace MH.Capstone.Domain.ApiContracts.Ninjas
{
    public record AnimalApiDto(string name, AnimalApiTaxonomyDto taxonomy,
        IEnumerable<string> locations, AnimalApiCharacteristics characteristics)
    { }

    public record AnimalApiTaxonomyDto(string kingdom, string phylum,
        string taxClass, string order, string family, string genus,
        string scientificName)
    { }

    public record AnimalApiCharacteristics(
        string prey,
        string nameOfYoung,
        string groupBehavior,
        string estimatedPopulationSize,
        string biggestThreat,
        string mostDistinctiveFeature,
        string gestationPeriod,
        string habitat,
        string diet,
        string averageLitterSize,
        string lifestyle,
        string commonName,
        string numberOfSpecies,
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
