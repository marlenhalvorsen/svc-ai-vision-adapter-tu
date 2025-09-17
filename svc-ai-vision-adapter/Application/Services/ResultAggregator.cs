using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Application.Services
{
    public class ResultAggregator : IResultAggregator
    {
        private readonly double _threshold;
        public ResultAggregator(double threshold = 0.7) => _threshold = threshold;

        public MachineAggregateDto Aggregate(IReadOnlyList<ShapedResultDto> list)
        {
            if (list.Count == 0) return new(null, null, null, 0, false);

            var best = list.OrderByDescending(x => x.Machine.Confidence).First();

            // Brand: første ikke-tomme brand
            var brand = list.Select(x => x.Machine.Brand)
                            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

            // Meget enkel model-heuristik fra OCR
            var ocr = list.Select(x => x.Evidence.OcrSample)
                          .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "";
            var model = System.Text.RegularExpressions.Regex
                .Matches(ocr, @"\b([A-Z]{1,3}\d{2,4}[A-Z]?)\b")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Groups[1].Value)
                .FirstOrDefault();

            var conf = best.Machine.Confidence;
            return new MachineAggregateDto(
                Brand: brand,
                Type: best.Machine.Type,
                Model: model,
                Confidence: conf,
                IsConfident: conf >= _threshold
            );
        }
    }
}
