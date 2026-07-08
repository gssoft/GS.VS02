using DS.BackgroundServices.Core04;

public interface ITunableSchedule : IBackgroundSchedule
{
    // Например, дать расписанию доступ к tuning-объекту
    void ApplyTuning(IBackgroundTuning tuning);
}
