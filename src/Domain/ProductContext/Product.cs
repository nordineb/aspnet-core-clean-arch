using System;
using System.Collections.Generic;
using System.Linq;
using Domain.ProductContext.Events;

namespace Domain.ProductContext
{
    public class Product : AggregateRoot<Guid>
    {
        private readonly ICollection<AttributeRef> _attributes = new HashSet<AttributeRef>();

        private readonly ICollection<Content> _contents = new HashSet<Content>();

        private Product()
        {
            Register<ProductCreated>(Apply);
            Register<ContentCreated>(Apply);
            Register<AttributeAddedToProduct>(Apply);
            Register<VariantCreated>(Apply);
        }
        
        public static Product Create(Guid productId,
            int categoryId,
            int brandId,
            string productCode)
        {
            var product =  new Product();
            product.ApplyChange(
                new ProductCreated(productId,
                    productCode,
                    brandId, categoryId));

            return product;
        }

        public BrandRef Brand { get; private set; }

        public CategoryRef Category { get; private set; }

        public string Code { get; private set; }

        public IReadOnlyCollection<Content> Contents => _contents.ToList();

        public IReadOnlyCollection<AttributeRef> Attributes => _attributes.ToList();

        private void Apply(ContentCreated @event) => _contents.Add(new Content(@event.Title, @event.Description, @event.SlicerAttribute));

        private void Apply(AttributeAddedToProduct @event) => _attributes.Add(@event.Attribute);

        private void Apply(VariantCreated @event) => _contents.First(c => c.SlicerAttribute == @event.SlicerAttribute).Route(@event);

        private void Apply(ProductCreated @event)
        {
            Id = @event.ProductId;
            Brand = new BrandRef(@event.BrandId, "");
            Category = new CategoryRef(@event.CategoryId, "");
            Code = @event.ProductCode;
        }
        
        public void CreateContent(string title, string description, AttributeRef slicerAttribute)
        {
            Should(() => _contents.Any() && _contents.All(c => c.HasSameTypeSlicerAttribute(slicerAttribute)),
                "Given attribute type should belong to any content of product as slicer");
            Should(() => _contents.All(c => c.SlicerAttribute != slicerAttribute),
                "Same content already exists with given attribute");

            ApplyChange(new ContentCreated(Id, title, description, slicerAttribute));
        }
        
        public void CreateVariant(string barcode, AttributeRef slicerAttribute, AttributeRef varianterAttribute)
        {
            var content = _contents.SingleOrDefault(c => c.SlicerAttribute == slicerAttribute);
            ShouldNot(content == null, "No content found with given slicer attribute.");
            
            // ReSharper disable once PossibleNullReferenceException
            var variantsOfContent = content.Variants;
           
            Should(() => variantsOfContent.Any() && variantsOfContent.All(c => c.HasSameTypeVarianterAttribute(varianterAttribute)),
                "Given attribute type should belong to any variant of product as varianter");
            
            Should(() => variantsOfContent.All(v => v.VarianterAttribute != varianterAttribute) ,
                "Same variant already exists with given attribute");
            
            ApplyChange(new VariantCreated(Id, barcode, slicerAttribute, varianterAttribute));
        }

        public void AddAttributeToContent(AttributeRef attribute)
        {
            Should(() => _attributes.Any(a => a != attribute),
                "Given attribute had already been added to the product");

            ApplyChange(new AttributeAddedToProduct(Id, attribute));
        }
    }

   
}