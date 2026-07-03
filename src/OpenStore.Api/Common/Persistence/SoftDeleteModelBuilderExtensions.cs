using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Common.Persistence;

internal static class SoftDeleteModelBuilderExtensions
{
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            MemberExpression property = Expression.Property(parameter, nameof(BaseEntity.IsActive));
            LambdaExpression filter = Expression.Lambda(property, parameter);

            entityType.SetQueryFilter(filter);
        }
    }
}
