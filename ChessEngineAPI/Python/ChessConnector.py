import chess
import chess.engine

engine = chess.engine.SimpleEngine.popen_uci(open("StockFishPath").read())

board = chess.Board()
while not board.is_game_over():
    UserMove = input("")
    board.push(chess.Move.from_uci(UserMove))
    result = engine.play(board, chess.engine.Limit(time=0.1))
    print(result.move) 
    board.push(result.move)

engine.quit()