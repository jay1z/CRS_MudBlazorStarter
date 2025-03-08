﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static CRS.Models.IReserveStudyElement;

namespace CRS.Models {
    public class ReserveStudyBuildingElement : BaseModel, IReserveStudyElement {
        public ReserveStudyBuildingElement() {
            ServiceContact ??= new ServiceContact();
        }
        public required Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public required Guid BuildingElementId { get; set; }
        public BuildingElement? BuildingElement { get; set; }
        public int Count { get; set; }
        public ServiceContact? ServiceContact { get; set; }
        [DataType(DataType.Date)] public DateTime? LastServiced { get; set; }
        public ElementMeasurementOptions? ElementMeasurementOptions { get; set; }
        public ElementRemainingLifeOptions? ElementRemainingLifeOptions { get; set; }
        public ElementUsefulLifeOptions? ElementUsefulLifeOptions { get; set; }

        [NotMapped]
        public List<ElementMeasurementOptions> ElementMeasurementOptionsList { get; set; } = new();
        [NotMapped]
        public List<ElementUsefulLifeOptions> ElementUsefulLifeOptionsList { get; set; } = new();
        [NotMapped]
        public List<ElementRemainingLifeOptions> ElementRemainingLifeOptionsList { get; set; } = new();

        [NotMapped]
        public ElementTypeEnum ElementType { get => ElementTypeEnum.Building; set => throw new NotImplementedException(); }

        [NotMapped]
        public string Name { get { return BuildingElement?.Name ?? throw new ArgumentNullException(); } set { } }

        [NotMapped]
        public bool NeedsService { get { return BuildingElement?.NeedsService ?? false; } set => throw new NotImplementedException();  }

        [NotMapped]
        public bool ShowDetails { get; set; } = false;
    }
}
