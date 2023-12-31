using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

namespace MrCMS.DbConfiguration.Conventions
{
    public class ReferencesConvention : IReferenceConvention
    {
        public void Apply(IManyToOneInstance instance)
        {
            instance.Cascade.SaveUpdate();

            instance.ForeignKey(
                $"FK_{instance.EntityType?.Name ?? instance.Name}_{instance.Property.Name}");
        }
    }
}