using System;
using Unity.Cinemachine;
using UnityEngine;

public class MapTransition : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundary;
    CinemachineConfiner2D confiner;
    [SerializeField] Direction direction;
    [SerializeField] Transform TeleportTargetPosition;
    [SerializeField] float posChange = 4;

    [SerializeField] bool transition = false;
    [SerializeField] String newMusic = null;
    enum Direction {Up, Down, Left, Right, Teleport}

    private void Awake()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            FadeTransition(collision.gameObject);
        }
    }

    async void FadeTransition(GameObject player)
    {
        if (transition)
        {
            await ScreenFader.Instance.FadeOut();
            confiner.BoundingShape2D = mapBoundary;
            updatePlayerPosition(player);
            if(newMusic != null)
            {
                MusicManager.Play(newMusic);
            }
            await ScreenFader.Instance.FadeIn();
        }

        else
        {
            confiner.BoundingShape2D = mapBoundary;
            updatePlayerPosition(player);
        }
    }

    private void updatePlayerPosition(GameObject player)
    {
        if(direction == Direction.Teleport)
        {
            player.transform.position = TeleportTargetPosition.position;
            return;
        }
        Vector3 newPos = player.transform.position;

        switch (direction)
        {
            case Direction.Up:
                newPos.y +=posChange;
                break;
            case Direction.Down:
                newPos.y -=posChange;
                break;
            case Direction.Left:
                newPos.x -=posChange;
                break;
            case Direction.Right:
                newPos.x +=posChange;
                break;
        }
        player.transform.position = newPos;
    }
}
