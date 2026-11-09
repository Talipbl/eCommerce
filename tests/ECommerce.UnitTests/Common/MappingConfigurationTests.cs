using ECommerce.Application;

namespace ECommerce.UnitTests.Common;

public class MappingConfigurationTests
{
    [Fact]
    public void Mapping_configuration_compiles()
    {
        var config = MappingRegistration.CreateConfig();

        Should.NotThrow(() => config.Compile());
    }
}
