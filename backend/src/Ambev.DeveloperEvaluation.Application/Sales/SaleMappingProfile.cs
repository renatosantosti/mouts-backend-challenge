using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class SaleMappingProfile : Profile
{
    public SaleMappingProfile()
    {
        CreateMap<SaleItem, SaleItemResponse>();
        CreateMap<Sale, SaleResponse>()
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));
    }
}
