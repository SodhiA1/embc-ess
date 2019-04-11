using Gov.Jag.Embc.Public.DataInterfaces;
using Gov.Jag.Embc.Public.Utils;
using Gov.Jag.Embc.Public.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Gov.Jag.Embc.Public.Controllers
{
    [Route("api/[controller]")]
    public class RegistrationsController : Controller
    {
        private readonly IDataInterface dataInterface;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger logger;
        private readonly IHostingEnvironment env;
        private readonly IUrlHelper urlHelper;
        private readonly IEmailSender emailSender;

        public RegistrationsController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IHostingEnvironment env,
            IDataInterface dataInterface,
            IEmailSender emailSender,
            IUrlHelper urlHelper
        )
        {
            this.emailSender = emailSender;
            this.dataInterface = dataInterface;
            this.httpContextAccessor = httpContextAccessor;
            logger = loggerFactory.CreateLogger(typeof(RegistrationsController));
            this.env = env;
            this.urlHelper = urlHelper;
        }

        [HttpGet(Name = nameof(GetAll))]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] SearchQueryParameters searchQuery)
        {
            try
            {
                var items = await dataInterface.GetRegistrationsAsync(searchQuery);

                return Json(new
                {
                    data = items.Items,
                    metadata = items.Pagination
                });
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                return BadRequest(e.ToString());
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOne(string id)
        {
            var result = await dataInterface.GetRegistrationAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Json(result);
        }

        [HttpGet("{id}/summary")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOneSummary(string id)
        {
            var result = await dataInterface.GetRegistrationSummaryAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Json(result);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] ViewModels.Registration item)
        {
            if (item != null && (!item.DeclarationAndConsent.HasValue || !item.DeclarationAndConsent.Value))
            {
                ModelState.AddModelError("DeclarationAndConsent", "Declaration And Consent must be set to 'True'");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                item.Id = null;
                item.Active = true;
                var result = await dataInterface.CreateRegistrationAsync(item);
                if (!string.IsNullOrWhiteSpace(result.HeadOfHousehold.Email))
                {
                    var registrationEmail = CreateEmailMessageForRegistration(result);
                    emailSender.Send(registrationEmail);
                }
                return Json(result);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                return BadRequest(e.ToString());
            }
        }

        private EmailMessage CreateEmailMessageForRegistration(Registration registration)
        {
            var essRegistrationLink = @"<a target='_blank' href='https://justice.gov.bc.ca/embcess/self-registration'>Evacuee Self-Registration</a>";
            var emergencyInfoBCLink = @"<a target='_blank' href='https://www.emergencyinfobc.gov.bc.ca/'>Emergency Info BC</a>";

            var subject = "Registration completed successfully";
            var body = $@"
This email has been generated by the Emergency Support Services program to provide you with a record of your ESS File Number.
If you have not registered online via the {essRegistrationLink} and are receiving this email, please contact your family members to ensure they have not registered on your behalf.
If you have received this email in error, please disregard.
<br/><br/>
Your ESS File Number is: <b>{registration.EssFileNumber}</b>
";

            // var body = "<h2>Evacuee Registration Success</h2><br/>" + "<b>What you need to know:</b><br/><br/>" +
            //    $"Your ESS File Number is: <b>{registration.EssFileNumber}</b>";

            if (registration.IncidentTask == null)
            {
                body += $@"
<br/><br/>
- If you are under order and require food, clothing, accommodation, transportation, incidentals or other emergency supports, proceed to your nearest Reception Centre.
A list of open Reception Centres can be found at {emergencyInfoBCLink}.<br/>
- If you do not require supports, or are under alert, no further actions are required.<br/>
- If you are a Reception Centre, proceed to one of the ESS volunteers on site who will be able to assist you with completing your registration.<br/>
- Your ESS File Number has been emailed to you.<br/>
- Please bring your ESS File Number with you to the Reception Centre.<br/>
";
            }

            return new EmailMessage(registration.HeadOfHousehold.Email, subject, body);
        }

        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Update([FromBody] ViewModels.Registration item, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || item == null || id != item.Id)
            {
                return BadRequest();
            }
            if (item != null && (!item.DeclarationAndConsent.HasValue || !item.DeclarationAndConsent.Value))
            {
                ModelState.AddModelError("DeclarationAndConsent", "Declaration And Consent must be set to 'True'");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await dataInterface.UpdateRegistrationAsync(item);
                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                return BadRequest(e.ToString());
            }
        }

        [HttpDelete("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            try
            {
                var result = await dataInterface.DeactivateRegistration(id);
                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                return BadRequest(e);
            }
        }
    }
}
