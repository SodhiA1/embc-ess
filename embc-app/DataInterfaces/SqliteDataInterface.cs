using Gov.Jag.Embc.Public.Utils;
using Gov.Jag.Embc.Public.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Gov.Jag.Embc.Public.DataInterfaces
{
    public class SqliteDataInterface : IDataInterface
    {
        public SqliteContext Db;//YT: this is a recipe for bad EF behavior, it should be created (and optionally disposed) in each method

        private readonly Func<SqliteContext> ctx;

        public SqliteDataInterface(string connectionString)
        {
            DbContextOptionsBuilder<SqliteContext> builder = new DbContextOptionsBuilder<SqliteContext>()
                .UseLazyLoadingProxies()
                .UseSqlite(connectionString);

            // init the database.
            Db = new SqliteContext(builder.Options);

            Db.Database.OpenConnection();

            ctx = () => new SqliteContext(builder.Options);
        }

        public Person CreatePerson(Person person)
        {
            // TODO: Implement
            throw new NotImplementedException();
            //return person;
        }

        #region Registration

        public async Task<Registration> CreateRegistrationAsync(Registration registration)
        {
            var db = ctx();
            var created = await db.Registrations.AddAsync(registration.ToModel());
            await db.SaveChangesAsync();

            return await GetRegistrationAsync(created.Entity.Id.ToString());
        }

        public async Task UpdateRegistrationAsync(Registration registration)
        {
            //var existing = Db.Registrations.FirstOrDefault(item => item.Id == new Guid(registration.Id));
            //if (existing != null)
            //{
            //    existing.PatchValues(registration);
            //    Db.Registrations.Update(existing);
            //    Db.SaveChanges();
            //    return Task.FromResult(existing.ToViewModel());
            //}
            //return Task.FromResult<Registration>(null);
            var db = ctx();
            db.Registrations.Update(registration.ToModel());
            await db.SaveChangesAsync();
        }

        public async Task<PaginatedList<Registration>> GetRegistrationsAsync(SearchQueryParameters queryParameters)
        {
            var db = ctx();
            IQueryable<Sqlite.Models.Registration> registrations = db.Registrations;
            var allItemCount = await registrations.CountAsync();

            if (queryParameters.HasSortBy())
            {
                // sort using dynamic linq extension method
                registrations = registrations.Sort(queryParameters.SortBy);
            }

            if (queryParameters.HasQuery())
            {
                // TODO: Implement FILTERING of search results!
                registrations = registrations.Where(item => this.SimpleSearch(item, queryParameters.Query));
            }

            var items = await registrations
                .Skip(queryParameters.Offset)
                .Take(queryParameters.Limit)
                .ToArrayAsync();

            return new PaginatedList<Registration>(items.Select(r => r.ToViewModel()), allItemCount, queryParameters.Offset, queryParameters.Limit);
        }

        private bool SimpleSearch(Sqlite.Models.Registration item, string q)
        {
            var byLastName = item.HeadOfHousehold?.LastName?.Contains(q, StringComparison.InvariantCultureIgnoreCase) ?? false;
            var byTaskNumber = item.IncidentTask?.TaskNumber?.Contains(q, StringComparison.InvariantCultureIgnoreCase) ?? false;
            var byEssFileNumber = item.EssFileNumber?.ToString().Contains(q, StringComparison.InvariantCultureIgnoreCase) ?? false;
            var byCommunity = (item.HeadOfHousehold?.PrimaryResidence as Sqlite.Models.BcAddress)?.Community?.Name?.Contains(q, StringComparison.InvariantCultureIgnoreCase) ?? false;

            // TODO: Add more of these...

            var filter = byLastName || byTaskNumber || byEssFileNumber || byCommunity;
            return filter;
        }

        private bool AdvancedSearch(Sqlite.Models.Registration item, string q)
        {
            // TODO: For NEXT RELEASE! - Advanced Search (out of scope for Release #1)
            throw new NotImplementedException();
        }

        public async Task<Registration> GetRegistrationAsync(string id)
        {
            var db = ctx();
            if (Guid.TryParse(id, out var guid))
            {
                var entity = await db.Registrations.FirstOrDefaultAsync(reg => reg.Id == guid);
                return entity?.ToViewModel();
            }
            return null;
        }

        #endregion Registration

        public async Task<Organization> GetOrganizationByBceidGuidAsync(string bceidGuid)
        {
            var item = await Db.Organizations.FirstOrDefaultAsync(x => x.BceidAccountNumber.Equals(bceidGuid, StringComparison.CurrentCultureIgnoreCase));
            var result = item.ToViewModel();
            return result;
        }

        public Person GetPersonByBceidGuid(string bceidGuid)
        {
            // TODO: Implement
            throw new NotImplementedException();
            //Person result = new Person();
            //return result;
        }

        public Volunteer GetVolunteerByName(string firstName, string lastName)
        {
            Volunteer result = null;
            var item = (Sqlite.Models.Volunteer)Db.People.FirstOrDefault(x => x.FirstName == firstName && x.LastName == lastName);
            if (item != null)
            {
                result = item.ToViewModel();
            }
            return result;
        }

        public List<Country> GetCountries()
        {
            List<Country> countries = new List<Country>();
            var countryList = Db.Countries.ToList();
            foreach (var country in countryList)
            {
                countries.Add(country.ToViewModel());
            }
            return countries;
        }

        public List<Region> GetRegions()
        {
            List<Region> regions = new List<Region>();
            var regionList = Db.Regions.ToList();
            foreach (var region in regionList)
            {
                regions.Add(region.ToViewModel());
            }
            return regions;
        }

        public List<RegionalDistrict> GetRegionalDistricts()
        {
            List<RegionalDistrict> regions = new List<RegionalDistrict>();
            var regionalDistrictList = Db.RegionalDistricts.ToList();
            foreach (var regionalDistrict in regionalDistrictList)
            {
                regions.Add(regionalDistrict.ToViewModel());
            }
            return regions;
        }

        public List<Community> GetCommunities()
        {
            List<Community> regions = new List<Community>();
            var communityList = Db.Communities.ToList();
            foreach (var community in communityList)
            {
                regions.Add(community.ToViewModel());
            }
            return regions;
        }

        public List<FamilyRelationshipType> GetFamilyRelationshipTypes()
        {
            var all = Db.FamilyRelationshipTypes.Select(x => x.ToViewModel()).ToList();
            return all;
        }

        //
        // Incident Tasks
        //

        public Task<List<IncidentTask>> GetIncidentTasks()
        {
            var all = Db.IncidentTasks.Select(task => task.ToViewModel()).ToListAsync();
            return all;
        }

        public Task<IncidentTask> GetIncidentTask(string id)
        {
            if (Guid.TryParse(id, out var guid))
            {
                var entity = Db.IncidentTasks.FirstOrDefault(task => task.Id == guid);
                return Task.FromResult(entity?.ToViewModel());
            }
            return Task.FromResult<IncidentTask>(null);
        }

        public Task<IncidentTask> CreateIncidentTask(IncidentTask task)
        {
            var entity = task.ToModel();
            Db.IncidentTasks.Add(entity);
            Db.SaveChanges();
            return Task.FromResult(entity.ToViewModel());
        }

        public Task<IncidentTask> UpdateIncidentTask(IncidentTask task)
        {
            var entity = Db.IncidentTasks.FirstOrDefault(item => item.Id == new Guid(task.Id));
            entity.PatchValues(task);
            Db.IncidentTasks.Update(entity);
            Db.SaveChanges();
            return Task.FromResult(entity.ToViewModel());
        }

        public Volunteer GetVolunteerByExternalId(string externalId)
        {
            Volunteer result = null;
            var item = (Sqlite.Models.Volunteer)Db.People.FirstOrDefault(x => ((Sqlite.Models.Volunteer)x).Externaluseridentifier == externalId);
            if (item != null)
            {
                result = item.ToViewModel();
            }
            return result;
        }

        public Volunteer GetVolunteerById(string id)
        {
            Volunteer result = null;
            Guid guid = new Guid(id);
            var item = (Sqlite.Models.Volunteer)Db.People.FirstOrDefault(x => x.Id == guid);
            if (item != null)
            {
                result = item.ToViewModel();
            }
            return result;
        }

        #region Organization

        public async Task<List<Organization>> GetOrganizationsAsync()
        {
            var db = ctx();
            var entities = await db.Organizations.ToListAsync();
            var result = new List<Organization>();
            foreach (var item in entities)
            {
                result.Add(item.ToViewModel());
            }
            return result;
        }

        public async Task<Organization> GetOrganizationAsync(string id)
        {
            var db = ctx();
            if (Guid.TryParse(id, out var guid))
            {
                var entity = await db.Organizations.FirstOrDefaultAsync(x => x.Id == guid);
                var result = entity.ToViewModel();
                return entity.ToViewModel();
            }
            return null;
        }

        public Organization GetOrganizationByLegalName(string name)
        {
            var db = ctx();
            var item = db.Organizations.FirstOrDefault(x => x.Name == name);
            var result = item.ToViewModel();

            return result;
        }

        public Organization GetOrganizationByExternalId(string externalId)
        {
            var db = ctx();
            var item = db.Organizations.FirstOrDefault(x => x.Externaluseridentifier == externalId);
            var result = item.ToViewModel();

            return result;
        }

        public async Task<Organization> CreateOrganizationAsync(Organization item)
        {
            var db = ctx();
            var entity = item.ToModel();
            await db.Organizations.AddAsync(entity);
            await db.SaveChangesAsync();

            return entity.ToViewModel();
        }

        public async Task<Organization> UpdateOrganizationAsync(Organization item)
        {
            var db = ctx();
            var entity = await db.Organizations.FirstOrDefaultAsync(x => x.Id == new Guid(item.Id));
            entity.PatchValues(item);
            db.Organizations.Update(entity);
            await db.SaveChangesAsync();

            return entity.ToViewModel();
        }

        public async Task<bool> DeactivateOrganizationAsync(string id)
        {
            var db = ctx();
            var entity = await db.Organizations.FirstOrDefaultAsync(x => x.Id == new Guid(id));
            if (entity == null)
            {
                return true;
            }
            entity.Active = false;
            db.Organizations.Update(entity);
            await db.SaveChangesAsync();

            return true;
        }

        #endregion Organization

        #region People

        private IQueryable<Sqlite.Models.Person> GetAllPeopleAsync(string type)
        {
            var db = ctx();
            return db.People.Where(p => p.PersonType.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Sqlite.Models.Person> GetSinglePersonByIdAsync(string type, string id)
        {
            var db = ctx();
            return await db.People.FirstOrDefaultAsync(p => p.PersonType.Equals(type, StringComparison.OrdinalIgnoreCase) && p.Id == Guid.Parse(id));
        }

        public async Task UpdatePersonAsync(Person person)
        {
            var db = ctx();
            db.People.Update(person.ToModel());
            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Person>> GetPeopleAsync(string type)
        {
            return await GetAllPeopleAsync(type).Select(p => p.ToViewModel()).ToArrayAsync();
        }

        public async Task<Person> GetPersonByIdAsync(string type, string id)
        {
            var person = await GetSinglePersonByIdAsync(type, id);
            if (person == null) return null;
            return person.ToViewModel();
        }

        public async Task<Person> CreatePersonAsync(Person person)
        {
            var db = ctx();
            var newPerson = await db.People.AddAsync(person.ToModel());
            await db.SaveChangesAsync();
            return newPerson.Entity.ToViewModel();
        }

        public async Task<bool> DeactivatePersonAsync(string type, string id)
        {
            var db = ctx();
            var person = await GetSinglePersonByIdAsync(type, id);
            if (person == null) return true;
            person.Active = false;
            db.Update(person);
            await db.SaveChangesAsync();
            return true;
        }

        #endregion People
    }
}
