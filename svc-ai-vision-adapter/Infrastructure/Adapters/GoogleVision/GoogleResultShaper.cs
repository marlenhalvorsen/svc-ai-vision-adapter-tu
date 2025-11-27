using System.Text.Json;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Application.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision.Parsing;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision.Resolvers;
using svc_ai_vision_adapter.Application.Models;
using svc_ai_vision_adapter.Application.Transport;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal sealed class GoogleResultShaper : IResultShaper
    {
        private readonly IBrandCatalog _brands;
        private readonly int _maxResults; //set to 5 to limit data
        private readonly GoogleVisionParser _parser;
        private readonly BrandResolver _brandResolver;
        private readonly TypeResolver _typeResolver;
        public GoogleResultShaper(IOptions<RecognitionOptions> options, 
            IBrandCatalog brands, 
            GoogleVisionParser parser,
            BrandResolver brandResolver,
            TypeResolver typeResolver)
        {
            _maxResults = options.Value.MaxResults;
            _brands = brands;
            _parser = parser;
            _brandResolver = brandResolver;
            _typeResolver = typeResolver;
        }
        public ShapedResultDto Shape(ProviderResultDto r)
        {
            //extract response from json output
            var resp = _parser.ExtractResponse(r);
            //fetches webEntites from the raw json material
            var web = _parser.GetWebEntities(resp, _maxResults);
            //fetches logos from the raw json material
            var logo = _parser.GetLogoHits(resp, _maxResults);
            //fetches textDetection from the raw json material 
            var ocr = _parser.GetOcrHits(resp);
            //resolve brand and brandscore
            var resolvedBrand = _brandResolver.ResolveBrand(logo.logo, logo.logoScore, web.entities, _brands, web.bestGuess, ocr);
            //see if any of the items from webEntities can be resolved to machineType
            var resolvedType = _typeResolver.ResolveType(web.bestGuess, web.entities, _brands);


            //all scores are already set with Math.Clamp to be in between 0-1
            double confidence = new[] { logo.logoScore, web.topScore, resolvedBrand.brandScore }.Max();
            var summary = new MachineSummaryDto(
                Type: resolvedType,
                Brand: resolvedBrand.resolvedBrand,
                Model: null,
                Confidence: confidence,
                IsConfident: confidence >= 0.5
            );

            var evidence = new EvidenceDto(
                WebBestGuess: web.bestGuess,
                Logo: logo.logo,
                OcrSample: ocr,
                WebEntities: web.entities,
                LogoCandidates: logo.logoCandidates
            );

            return new ShapedResultDto(r.ImageRef, summary, evidence);
        }
    }
}
