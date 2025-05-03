using AutoMapper;
using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Application.Dtos;

namespace MetalFlowScheduler.Api.Application.Mappers
{
    /// <summary>
    /// Define os perfis de mapeamento para o AutoMapper entre Entidades e DTOs.
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Mapeamentos Entidade -> DTO ---

            CreateMap<OperationType, OperationTypeDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID));

            CreateMap<Operation, OperationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID))
                .ForMember(dest => dest.OperationTypeName, opt => opt.MapFrom(src => src.OperationType != null ? src.OperationType.Name : null))
                .ForMember(dest => dest.WorkCenterName, opt => opt.MapFrom(src => src.WorkCenter != null ? src.WorkCenter.Name : null));

            CreateMap<WorkCenter, WorkCenterDto>()
                 .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID))
                 .ForMember(dest => dest.LineName, opt => opt.MapFrom(src => src.Line != null ? src.Line.Name : null));

            CreateMap<Line, LineDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID));

            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID));


            // --- Mapeamentos DTO -> Entidade (para criação/atualização) ---

            // OperationType
            CreateMap<CreateOperationTypeDto, OperationType>();
            CreateMap<UpdateOperationTypeDto, OperationType>()
                 // Ignora propriedades gerenciadas pela BaseEntity/Repositório ou navegações
                 .ForMember(dest => dest.ID, opt => opt.Ignore())
                 .ForMember(dest => dest.Enabled, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastUpdate, opt => opt.Ignore())
                 .ForMember(dest => dest.Operations, opt => opt.Ignore())
                 .ForMember(dest => dest.WorkCenterRoutes, opt => opt.Ignore())
                 .ForMember(dest => dest.ProductRoutes, opt => opt.Ignore());


            // Operation
            CreateMap<CreateOperationDto, Operation>();
            CreateMap<UpdateOperationDto, Operation>()
                 .ForMember(dest => dest.ID, opt => opt.Ignore())
                 .ForMember(dest => dest.Enabled, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastUpdate, opt => opt.Ignore())
                 .ForMember(dest => dest.OperationType, opt => opt.Ignore()) // Ignora navegação, usa OperationTypeId
                 .ForMember(dest => dest.WorkCenter, opt => opt.Ignore());   // Ignora navegação, usa WorkCenterId

            // WorkCenter
            CreateMap<CreateWorkCenterDto, WorkCenter>()
                 .ForMember(dest => dest.OperationRoutes, opt => opt.Ignore()) // Rotas gerenciadas pelo serviço
                 .ForMember(dest => dest.Line, opt => opt.Ignore()); // Ignora navegação, usa LineId
            CreateMap<UpdateWorkCenterDto, WorkCenter>()
                 .ForMember(dest => dest.ID, opt => opt.Ignore())
                 .ForMember(dest => dest.Enabled, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastUpdate, opt => opt.Ignore())
                 .ForMember(dest => dest.Line, opt => opt.Ignore())
                 .ForMember(dest => dest.Operations, opt => opt.Ignore())
                 .ForMember(dest => dest.LineRoutes, opt => opt.Ignore())
                 .ForMember(dest => dest.OperationRoutes, opt => opt.Ignore()) // Rotas gerenciadas pelo serviço
                 .ForMember(dest => dest.SurplusStocks, opt => opt.Ignore());

            // Line
            CreateMap<CreateLineDto, Line>()
                 .ForMember(dest => dest.WorkCenterRoutes, opt => opt.Ignore()) // Rotas gerenciadas pelo serviço
                 .ForMember(dest => dest.AvailableProducts, opt => opt.Ignore()); // Disponibilidade gerenciada pelo serviço
            CreateMap<UpdateLineDto, Line>()
                 .ForMember(dest => dest.ID, opt => opt.Ignore())
                 .ForMember(dest => dest.Enabled, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastUpdate, opt => opt.Ignore())
                 .ForMember(dest => dest.WorkCenters, opt => opt.Ignore())
                 .ForMember(dest => dest.WorkCenterRoutes, opt => opt.Ignore()) // Rotas gerenciadas pelo serviço
                 .ForMember(dest => dest.AvailableProducts, opt => opt.Ignore()); // Disponibilidade gerenciada pelo serviço

            // Product
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.OperationRoutes, opt => opt.Ignore()); // Rotas gerenciadas pelo serviço
            CreateMap<UpdateProductDto, Product>()
                 .ForMember(dest => dest.ID, opt => opt.Ignore())
                 .ForMember(dest => dest.Enabled, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastUpdate, opt => opt.Ignore())
                 .ForMember(dest => dest.OperationRoutes, opt => opt.Ignore()) // Rotas gerenciadas pelo serviço
                 .ForMember(dest => dest.AvailableOnLines, opt => opt.Ignore())
                 .ForMember(dest => dest.ProductionOrderItems, opt => opt.Ignore())
                 .ForMember(dest => dest.SurplusStocks, opt => opt.Ignore());
        }
    }
}
