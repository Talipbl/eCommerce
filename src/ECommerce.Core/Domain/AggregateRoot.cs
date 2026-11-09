namespace ECommerce.Core.Domain
{
    public class AggregateRoot<TId> : IEntity, IAuditableEntity
    {
        public TId Id { get; set; } = default!;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }

        public AggregateRoot()
        {
        }

        public AggregateRoot(TId id)
        {
            Id = id;
        }
    }
}
