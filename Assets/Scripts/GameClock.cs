
using TMPro;
using UnityEngine;

public class GameClock : MonoBehaviour
{
    [Header("Interface")]
    [SerializeField] private TMP_Text clockText;

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

    private void Start()
    {
        // Converte o horario inicial para minutos
        // desde meia-noite.
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
        if (clockText == null)
            return;

        float secondsPerMinute =
            Mathf.Max(0.01f, realSecondsPerGameMinute);

        int elapsedGameMinutes =
            Mathf.FloorToInt(
                elapsedRealSeconds / secondsPerMinute
            );

        // O dia se repete a cada 24 horas.
        int currentMinutes =
            (initialGameMinutes + elapsedGameMinutes)
            % (24 * 60);

        int hour24 = currentMinutes / 60;
        int minute = currentMinutes % 60;

        string period = hour24 < 12 ? "AM" : "PM";

        int hour12 = hour24 % 12;

        if (hour12 == 0)
            hour12 = 12;

        clockText.text =
            $"{hour12:00}:{minute:00} {period}";
    }
}
