using svc_ai_vision_adapter.Application.Contracts;
using System.Text.RegularExpressions;

namespace svc_ai_vision_adapter.Application.Services
{
    public class ResultAggregatorService : IResultAggregator
    {
        private readonly double _threshold;
        public ResultAggregatorService(double threshold = 0.5) => _threshold = threshold;

        //Looks for patterns that could be a modelcode like 930G or D67 from the OCR
        static readonly Regex DigitsFirst = new(@"\b\d{2,4}[A-Z]{1,2}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex LettersFirst = new(@"\b[A-Z]{1,3}[ \t-]?\d{2,4}(?:-\d{1,2})?[A-Z]{0,2}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        static readonly HashSet<string> Canonical = new(StringComparer.OrdinalIgnoreCase)
        {
            "Wheel Loader", "Loader", 
            "Excavator", "Bulldozer", 
            "Motor Grader", "Dump Truck", 
            "Backhoe Loader"
        };

        private static (string? Type, double? TypeConfidence, string? Source)PickTypeWithEvidence(IReadOnlyList<ShapedResultDto> list)
        {
            const double entityMinScore = 0.6;
            //checks for entity hit first - marks it as web_entity for trace¨.
            //Uses entitys as this feature comes with a score - so we can set threshold later
            var entityHit = list
                .SelectMany(x => x.Evidence.WebEntities ?? Enumerable.Empty<WebEntityHitDto>())
                .OrderByDescending(e => e.Score)
                .FirstOrDefault(e => e.Score >=entityMinScore &&
                Canonical.Any(t => e.Description.IndexOf(t, StringComparison.OrdinalIgnoreCase) >=0));
            if(entityHit is not null)
            {
                var matchedType = Canonical.First(t =>
                entityHit.Description.IndexOf(t, StringComparison.OrdinalIgnoreCase) >=0);

                return (matchedType, entityHit.Score, "web_entity");
            }

            //Checks for canonical types among objects
            const double objMinScore = 0.5;
            var objType = list
               .SelectMany(x => x.Evidence.Objects ?? Array.Empty<ObjectHitDto>())
               .Where(o => o.Score >= objMinScore)
               .Select(o => new
               {
                   o.Score,
                   Match = Canonical.FirstOrDefault(t =>
               o.Name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
               })
               .Where(x => x.Match is not null)
               .OrderByDescending(x => x.Score)
               .FirstOrDefault();
               
            if (objType is not null)
                return (objType.Match, objType.Score, "object_localization");

            //if webentity or object is not found check for best label, then mark as web_best_guess
            var bestGuess = (list.Select(x=>x.Evidence.WebBestGuess)
                .FirstOrDefault(s=> !string.IsNullOrWhiteSpace(s)) ?? "")
                .ToLowerInvariant();
            foreach(var t in Canonical)
                if (bestGuess.Contains(t.ToLowerInvariant())) 
                    return (t, null, "web_best_guess");


            // C) else return null
            return (null, null, null);
        }

        public MachineAggregateDto Aggregate(IReadOnlyList<ShapedResultDto> list)
        {
            //if no results, then return an empty object
            if (list.Count == 0) return new MachineAggregateDto{
                Brand = null, Type = null, Model = null, 
                Confidence = 0, IsConfident = false,
                TypeConfidence = 0, TypeSource = null };

            var best = list.OrderByDescending(x => x.Machine.Confidence).First();

            // Brand: first !null brand
            var brand = list.Select(x => x.Machine.Brand)
                            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            var ocr = list.Select(x => x.Evidence.OcrSample)
              .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? string.Empty;

            var m1 = DigitsFirst.Match(ocr);               // finds "930G"
            var m2 = m1.Success ? Match.Empty : LettersFirst.Match(ocr); // finds ex "EC220E" (without newline)
            var model = (m1.Success ? m1.Value : (m2.Success ? m2.Value : null))?.ToUpperInvariant();

            //finds canonical type with score and source by using the ShapedResultDto List
            var (typeChosen, typeConfidence, typeSource) = PickTypeWithEvidence(list);
            var type = Canonical.Contains(typeChosen ?? "") ? typeChosen : null;//when type not canonical return null, empty string to ensure Contains works
            var detectionConfidence = best.Machine.Confidence; //says something about how certain it is of objects in picture
            var isImageUsable = detectionConfidence >= _threshold;
            return new MachineAggregateDto
            {
                Brand = brand,
                Type = type,
                Model = model,
                Confidence = detectionConfidence,
                IsConfident = isImageUsable,
                TypeConfidence = typeConfidence,     
                TypeSource = typeSource         
            };
        }
    }
}
