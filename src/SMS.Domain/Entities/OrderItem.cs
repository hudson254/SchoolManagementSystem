using SMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// A line item on an OMS order. Quantity/unit price/line total are set only
    /// through the factory or <see cref="Update"/> — never trusted from a client.
    /// Monetary arithmetic is decimal with half-even rounding (<see cref="OmsMoney"/>).
    /// </summary>
    [Table("oms_order_items")]
    public class OrderItem : BaseEntity, ITenantAwareEntity
    {
        [Column("order_id")]
        public Guid OrderId { get; private set; }

        [MaxLength(50)]
        public string? ItemCode { get; private set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; private set; } = string.Empty;

        /// <summary>Quantity must be greater than zero (open requirement #9).</summary>
        public int Quantity { get; private set; }

        /// <summary>Unit price must not be negative; default 0 when omitted (open requirement #9).</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal UnitPrice { get; private set; }

        /// <summary>Derived: Quantity × UnitPrice (rounded to currency scale).</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal LineTotal { get; private set; }

        /// <summary>Ordinal row number (1-based), also used for CSV import traceability.</summary>
        [Column("row_number")]
        public int RowNumber { get; private set; }

        public virtual Order Order { get; private set; } = null!;

        private OrderItem()
        {
        }

        /// <summary>Creates a validated order item.</summary>
        public static OrderItem Create(string? itemCode, string description, int quantity, decimal unitPrice)
        {
            Validate(description, quantity, unitPrice);

            if (!string.IsNullOrEmpty(itemCode) && itemCode.Length > 50)
            {
                throw new ArgumentException("Item code must not exceed 50 characters.", nameof(itemCode));
            }

            var item = new OrderItem
            {
                ItemCode = itemCode,
                Description = description,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
            item.Recalculate();
            return item;
        }

        /// <summary>Updates quantity/unit price (used by the later edit/import flows).</summary>
        public void Update(int quantity, decimal unitPrice)
        {
            Validate(Description, quantity, unitPrice);
            Quantity = quantity;
            UnitPrice = unitPrice;
            Recalculate();
        }

        /// <summary>Assigns the 1-based row number (called by the owning aggregate).</summary>
        internal void AssignRowNumber(int rowNumber)
        {
            if (rowNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowNumber), "Row number must be greater than zero.");
            }

            RowNumber = rowNumber;
        }

        private static void Validate(string description, int quantity, decimal unitPrice)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Item description is required.", nameof(description));
            }

            if (description.Length > 500)
            {
                throw new ArgumentException("Item description must not exceed 500 characters.", nameof(description));
            }

            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
            }

            if (unitPrice < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must not be negative.");
            }
        }

        private void Recalculate() => LineTotal = OmsMoney.LineTotal(Quantity, UnitPrice);
    }
}
