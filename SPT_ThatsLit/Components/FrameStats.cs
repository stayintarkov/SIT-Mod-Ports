namespace ThatsLit.Components
{
    public struct FrameStats
    {
        public int pxS, pxH, pxHM, pxM, pxML, pxL, pxD;
        public int brighterPixels, darkerPixels;
        public float avgLum, avgLumMultiFrames;
        public float avgLumNonDark;
        public int pixels;
        public float score, ambienceScore;
        public float multiFrameLitScore;

        public float RatioShinePixels => pxS / (float)pixels;
        public float RatioHighPixels => pxH / (float)pixels;
        public float RatioHighMidPixels => pxHM / (float)pixels;
        public float RatioMidPixels => pxM / (float)pixels;
        public float RatioMidLowPixels => pxML / (float)pixels;
        public float RatioLowPixels => pxL / (float)pixels;
        public float RatioDarkPixels => pxD / (float)pixels;
        public float RatioLowAndDarkPixels => (pxL + pxD) / (float)pixels;
        public int BrighterPixels => pxS + pxH + pxHM + pxM / 2;
        public float RatioBrighterPixels => BrighterPixels / (float)pixels;
        public int DarkerPixels => pxM / 2 + pxML + pxL + pxD;

    }
}