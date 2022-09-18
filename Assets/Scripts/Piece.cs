using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public int rotationIndex { get; private set; }

    public float stepDelay = 1f;
    public float moveDelay = 0.1f;
    public float lockDelay = 0.5f;

    private float delayMoveDelay = 0f;
    private int step = 0;

    private float stepTime;
    private float moveTime;
    private float lockTime;

    private bool isRotateLeftPushed;
    private bool isRotateRightPushed;

    private bool isLeftPushed;
    private bool isRightPushed;

    private bool isHardDownPushed;
    private bool isDownPushed;

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        this.data = data;
        this.board = board;
        this.position = position;

        rotationIndex = 0;
        stepTime = Time.time + stepDelay;
        moveTime = Time.time + moveDelay;
        lockTime = 0f;

        if (cells == null)
        {
            cells = new Vector3Int[data.cells.Length]; //������ 4
        }

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = (Vector3Int)data.cells[i];
        }
    }

    private void Update()
    {
        board.Clear(this);

        // �� ���������� ������, ����� ��������� ������ ������� ���������� � ������
        // �� ����, ��� �� ������������� �� �����
        lockTime += Time.deltaTime;

        // �������� �����
        if (isRotateLeftPushed)
        {
            Rotate(-1);
            isRotateLeftPushed = false;
        }
        else if (isRotateRightPushed)
        {
            Rotate(1);
            isRotateRightPushed = false;
        }

        // ������� ���� � ����� ����� ����� ���
        if (isHardDownPushed)
        {
            HardDrop();
            isHardDownPushed = false;
        }

        // ��������� ������ ���������� ������� �����������, �� ������ ����� �������� ����
        // ����� �� �� �������� ������� ������
        if (Time.time > moveTime + delayMoveDelay)
        {
            HandleMoveInputs();
        }

        // ����������� ������ � ��������� ��� ������ X ������
        if (Time.time > stepTime)
        {
            Step();
        }

        board.Set(this);
    }

    private void HandleMoveInputs()
    {
        // �������� ���� �� �������
        if (isDownPushed)
        {
            step++;
            if (Move(Vector2Int.down))
            {
                // �������� ����� ����, ����� ������������� ������� �����������
                stepTime = Time.time + stepDelay;
            }
        }

        // �������� �����/������
        if (isLeftPushed)
        {
            step++;
            Move(Vector2Int.left);
        }
        else if (isRightPushed)
        {
            step++;
            Move(Vector2Int.right);
        }
    }

    public void OnLeftButtonDown()
    {
        step = 0;
        isLeftPushed = true;
    }

    public void OnRightButtonDown()
    {
        step = 0;
        isRightPushed = true;
    }

    public void OnRotateLeftButtonDown()
    {
        isRotateLeftPushed = true;
    }

    public void OnRotateRightButtonDown()
    {
        isRotateRightPushed = true;
    }

    public void OnHardDropButtonDown()
    {
        isHardDownPushed = true;
    }

    public void OnDownButtonDown()
    {
        step = 0;
        isDownPushed = true;
    }

    public void OnLeftButtonUp()
    {
        isLeftPushed = false;
        delayMoveDelay = 0f;
    }

    public void OnRightButtonUp()
    {
        isRightPushed = false;
        delayMoveDelay = 0f;
    }

    public void OnDownButtonUp()
    {
        isDownPushed = false;
        delayMoveDelay = 0f;
    }

    private void Step()
    {
        stepTime = Time.time + stepDelay;

        // ��������� � ���������� ����
        Move(Vector2Int.down);

        // ��� ������ ������� ��������� ������� �����, �� ���������� ���������������
        if (lockTime >= lockDelay)
        {
            Lock();
        }
    }

    private void HardDrop()
    {
        while(Move(Vector2Int.down))
        {
            continue;
        }

        Lock();
    }

    private void Lock()
    {
        board.Set(this);
        board.ClearLines();
        board.SpawnPiece();
    }

    private bool Move(Vector2Int translation)
    {
        Vector3Int newPosition = position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;

        bool valid = board.IsValidPosition(this, newPosition);

        // ���������� �������� ������ � ��� ������, ���� ����� ������� �������������
        if (valid)
        {
            position = newPosition;
            moveTime = Time.time + moveDelay;
            lockTime = 0f; // ���������� �������
        }
        
        if ((delayMoveDelay == 0f && step == 1) && (isLeftPushed || isRightPushed || isDownPushed) )
        {
            delayMoveDelay = 0.1f;
        }
        else delayMoveDelay = 0f;

        return valid;
    }

    private void Rotate(int direction)
    {
        // ��������� ������� �������� �� ������ ���� ��������
        // � ��� ����� ���������
        int originalRotation = rotationIndex;

        // ��������� ��� ������ �����, ��������� ������� ��������
        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        // �������� ��������, ���� ����� �� ���� �� ����� ���������� ��������
        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = Data.RotationMatrix; // 0 1 -1 0

        // ��������� ��� ������, ��������� ������� ��������
        for (int i = 0; i < cells.Length; i++) // 4 ��������
        {
            Vector3 cell = cells[i];

            int x, y;

            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    // "I" � "O" �������������� �� ��������� ����������� �����
                    cell.x = cell.x - 0.5f;
                    cell.y = cell.y - 0.5f;
                    x = Mathf.CeilToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;

                default:
                    x = Mathf.RoundToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
            }

            cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);

        for (int i = 0; i < data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];

            if (Move(translation))
            {
                return true;
            }
        }

        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;

        if (rotationDirection < 0)
        {
            wallKickIndex--;
        }

        return Wrap(wallKickIndex, 0, data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - min);
        }
    }

}