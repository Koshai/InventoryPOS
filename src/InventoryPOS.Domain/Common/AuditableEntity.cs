using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryPOS.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public Guid? CreatedByUserId { get; protected set; }

    public Guid? UpdatedByUserId { get; protected set; }

    public void MarkUpdated(Guid? userId)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedByUserId = userId;
    }
}
