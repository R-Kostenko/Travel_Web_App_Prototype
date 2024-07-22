using Models;

namespace Services
{
    public class ScrollService
    {
        public ScrollData ScrollData { get; set; } = new();
        public Action<ScrollData> OnScroll { get; set; }
        public void ScrollUpdate(ScrollData scrollData)
        {
            ScrollData = scrollData;
            OnScroll?.Invoke(ScrollData);
        }
    }
}
