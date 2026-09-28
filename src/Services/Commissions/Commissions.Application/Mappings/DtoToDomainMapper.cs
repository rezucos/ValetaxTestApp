using ValetaxTest.Commissions.Application.Commissions;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Application.Mappings;

public static class DtoToDomainMapper
{
    extension(SwitchSchemaRequest request)
    {
        /// <summary>Schema named in the request; the request must be validated first.</summary>
        public CommissionSchemaType ToDomain() => Enum.Parse<CommissionSchemaType>(request.SchemaType!, ignoreCase: true);
    }
}
