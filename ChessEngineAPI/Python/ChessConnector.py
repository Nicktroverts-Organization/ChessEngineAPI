import chess
import chess.engine

engine = chess.engine.SimpleEngine.popen_uci(r"C:\Users\Praktikant\Downloads\stockfish-windows-x86-64-avx2\stockfish\stockfish-windows-x86-64-avx2.exe")

board = chess.Board()
while not board.is_game_over():
    UserMove = input("")
    board.push(chess.Move.from_uci(UserMove))
    result = engine.play(board, chess.engine.Limit(time=0.1))
    print(result.move) 
    board.push(result.move)

engine.quit()