using ApplicationSaleResponse = Ambev.DeveloperEvaluation.Application.Sales.Common.SaleResponse;
using ApplicationSaleItemResponse = Ambev.DeveloperEvaluation.Application.Sales.Common.SaleItemResponse;
using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using WebApiSaleResponse = Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common.SaleResponse;
using WebApiSaleItemResponse = Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common.SaleItemResponse;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

public sealed class SalesProfile : Profile
{
    public SalesProfile()
    {
        CreateMap<CreateSaleRequest, CreateSaleCommand>();
        CreateMap<ApplicationSaleItemResponse, WebApiSaleItemResponse>();
        CreateMap<ApplicationSaleResponse, WebApiSaleResponse>()
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));
    }
}
