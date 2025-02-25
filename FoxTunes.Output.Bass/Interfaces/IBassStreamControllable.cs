namespace FoxTunes.Interfaces
{
    public interface IBassStreamControllable
    {
        void PreviewPlay();

        void PreviewPause();

        void PreviewResume();

        void PreviewStop();

        void Play();

        void Pause();

        void Resume();

        void Stop();
    }
}
