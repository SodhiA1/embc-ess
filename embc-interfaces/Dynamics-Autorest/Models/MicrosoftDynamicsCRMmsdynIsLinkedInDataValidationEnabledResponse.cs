// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Gov.Jag.Embc.Interfaces.Models
{
    using Newtonsoft.Json;
    using System.Linq; using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// msdyn_IsLinkedInDataValidationEnabledResponse
    /// </summary>
    public partial class MicrosoftDynamicsCRMmsdynIsLinkedInDataValidationEnabledResponse
    {
        /// <summary>
        /// Initializes a new instance of the
        /// MicrosoftDynamicsCRMmsdynIsLinkedInDataValidationEnabledResponse
        /// class.
        /// </summary>
        public MicrosoftDynamicsCRMmsdynIsLinkedInDataValidationEnabledResponse()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the
        /// MicrosoftDynamicsCRMmsdynIsLinkedInDataValidationEnabledResponse
        /// class.
        /// </summary>
        public MicrosoftDynamicsCRMmsdynIsLinkedInDataValidationEnabledResponse(bool? result = default(bool?))
        {
            Result = result;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Result")]
        public bool? Result { get; set; }

    }
}
