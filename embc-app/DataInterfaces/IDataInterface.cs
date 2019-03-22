using Gov.Jag.Embc.Public.Utils;
using Gov.Jag.Embc.Public.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gov.Jag.Embc.Public.DataInterfaces
{
    public interface IDataInterface
    {
        Person GetPersonByBceidGuid(string bceidGuid);

        Volunteer GetVolunteerByName(string firstName, string lastName);

        Person CreatePerson(Person person);

        // Registrations
        Task<PaginatedList<Registration>> GetRegistrationsAsync(SearchQueryParameters queryParameters);

        Task<Registration> GetRegistrationAsync(string id);

        Task<Registration> CreateRegistrationAsync(Registration registration);

        Task UpdateRegistrationAsync(Registration registration);

        // Incident Tasks
        Task<List<IncidentTask>> GetIncidentTasks();

        Task<IncidentTask> GetIncidentTask(string id);

        Task<IncidentTask> CreateIncidentTask(IncidentTask task);

        Task<IncidentTask> UpdateIncidentTask(IncidentTask task);

        List<Community> GetCommunities();

        List<Country> GetCountries();

        List<Region> GetRegions();

        List<RegionalDistrict> GetRegionalDistricts();

        List<FamilyRelationshipType> GetFamilyRelationshipTypes();

        #region Organization

        //Organizations
        Task<List<Organization>> GetOrganizationsAsync();

        Task<Organization> GetOrganizationAsync(string id);

        Task<Organization> GetOrganizationByBceidGuidAsync(string bceidGuid);

        Organization GetOrganizationByLegalName(string name);

        Organization GetOrganizationByExternalId(string externalId);

        Task<Organization> CreateOrganizationAsync(Organization item);

        Task<Organization> UpdateOrganizationAsync(Organization item);

        Task<bool> DeactivateOrganizationAsync(string id);

        #endregion Organization

        Volunteer GetVolunteerByExternalId(string externalId);

        Volunteer GetVolunteerById(string Id);

        Task<IEnumerable<Person>> GetPeopleAsync(string type);

        Task<Person> GetPersonByIdAsync(string type, string id);

        Task UpdatePersonAsync(Person person);

        Task<Person> CreatePersonAsync(Person person);

        Task<bool> DeactivatePersonAsync(string type, string id);
    }
}
