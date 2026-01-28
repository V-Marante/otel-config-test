using Microsoft.Azure.CosmosRepository;
using Microsoft.Azure.CosmosRepository.Attributes;

namespace cosmos_repository_loggingtests
{
    [PartitionKeyPath("/tenantId")]
    public class Person : Item
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string TenantId { get; set; }

        protected override string GetPartitionKeyValue()
        {
            return TenantId;
        }
    }
}