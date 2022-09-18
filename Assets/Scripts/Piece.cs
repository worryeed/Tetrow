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
            cells = new Vector3Int[data.cells.Length]; //Всегда 4
        }

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = (Vector3Int)data.cells[i];
        }
    }

    private void Update()
    {
        board.Clear(this);

        // Мы используем таймер, чтобы позволить игроку вносить коррективы в фигуру
        // до того, как он зафиксируется на месте
        lockTime += Time.deltaTime;

        // Вращение ручки
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

        // Уронить блок в самый самый самый низ
        if (isHardDownPushed)
        {
            HardDrop();
            isHardDownPushed = false;
        }

        // Разрешить игроку удерживать клавиши перемещения, но только после задержки хода
        // чтобы он не двигался слишком быстро
        if (Time.time > moveTime + delayMoveDelay)
        {
            HandleMoveInputs();
        }

        // Перемещайте фигуру в следующий ряд каждые X секунд
        if (Time.time > stepTime)
        {
            Step();
        }

        board.Set(this);
    }

    private void HandleMoveInputs()
    {
        // Движение вниз на единицу
        if (isDownPushed)
        {
            step++;
            if (Move(Vector2Int.down))
            {
                // Обновите время шага, чтобы предотвратить двойное перемещение
                stepTime = Time.time + stepDelay;
            }
        }

        // Движение влево/вправо
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

        // Перейдите к следующему ряду
        Move(Vector2Int.down);

        // Как только элемент неактивен слишком долго, он становится заблокированным
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

        // Сохраняйте движение только в том случае, если новая позиция действительна
        if (valid)
        {
            position = newPosition;
            moveTime = Time.time + moveDelay;
            lockTime = 0f; // перезапуск времени
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
        // Сохранить текущее вращение на случай сбоя вращения
        // и нам нужно вернуться
        int originalRotation = rotationIndex;

        // Поверните все ячейки блока, используя матрицу вращения
        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        // Отмените вращение, если тесты на удар по стене завершатся неудачей
        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = Data.RotationMatrix; // 0 1 -1 0

        // Поверните все ячейки, используя матрицу вращения
        for (int i = 0; i < cells.Length; i++) // 4 итерации
        {
            Vector3 cell = cells[i];

            int x, y;

            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    // "I" и "O" поворачиваются от смещенной центральной точки
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