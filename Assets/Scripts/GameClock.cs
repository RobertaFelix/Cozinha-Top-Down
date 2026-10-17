
using TMPro;
using UnityEngine;

public class GameClock : MonoBehaviour
{
    [Header("Interface")]
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private TMP_Text weekdayText;
    [SerializeField] private TMP_Text dayNumberText;

    [Header("Horario inicial")]
    [Range(1, 12)]
    [SerializeField] private int startHour = 8;

    [Range(0, 59)]
    [SerializeField] private int startMinute = 0;

    [SerializeField] private bool startPM = false;

    [Header("Velocidade do tempo")]
    [SerializeField] private float realSecondsPerGameMinute = 5f;

    private float elapsedRealSeconds;
    private int initialGameMinutes;

    private readonly string[] weekdays =
    {
        "MONDAY",
        "TUESDAY",
        "WEDNESDAY",
        "THURSDAY",
        "FRIDAY",
        "SATURDAY",
        "SUNDAY"
    };

    private void Start()
    {
        int hour24 = startHour % 12;

        if (startPM)
        {
            hour24 += 12;
        }

        initialGameMinutes =
            hour24 * 60 + startMinute;

        elapsedRealSeconds = 0f;

        UpdateClock();
    }

    private void Update()
    {
        elapsedRealSeconds += Time.deltaTime;

        UpdateClock();
    }

    private void UpdateClock()
    {

        float safeSecondsPerMinute =
            Mathf.Max(0.01f, realSecondsPerGameMinute);

        int elapsedGameMinutes =
            Mathf.FloorToInt(
                elapsedRealSeconds / safeSecondsPerMinute
            );

        int totalGameMinutes =
            initialGameMinutes + elapsedGameMinutes;

        // Hora atual dentro do ciclo de 24 horas.
        int currentMinutes =
            totalGameMinutes % (24 * 60);

        int hour24 = currentMinutes / 60;
        int minute = currentMinutes % 60;

        string period = hour24 < 12 ? "AM" : "PM";

        int hour12 = hour24 % 12;

        if (hour12 == 0)
        {
            hour12 = 12;
        }

        if (clockText != null)
        {
            clockText.text =
                $"{hour12:00}:{minute:00} {period}";
        }

        // Quantos dias completos se passaram desde o início.
        int elapsedDays =
            totalGameMinutes / (24 * 60);

        // Semana que se repete a cada 7 dias.
        int weekdayIndex = elapsedDays % 7;

        if (weekdayText != null)
        {
            weekdayText.text = weekdays[weekdayIndex];
        }

        // Calendário de 30 dias: 01 até 30, depois volta a 01.
        int dayNumber = (elapsedDays % 30) + 1;

        if (dayNumberText != null)
        {
            dayNumberText.text = dayNumber.ToString("D2");
        }
    }
}
