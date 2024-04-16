# ChessEngineAPI

## **How to setup**
1. Install python.
2. Open terminal and run "pip install chess".
3. Initialize and save the Port.
4. CreateInstance and save the Key.
5. You can now use this api.

## **Commands:**
- INITIALIZE~

      Initializes the api and sets the listener to a new, free, Port.
- CREATEINSTANCE~

      Creates a new instance and responds with the key to that instance.
- MAKEMOVE~{key}~{from}{to}

      Makes a move in the chess engine of the specified instance(key).
- GETENGINEMOVE~{key}~

      Gets the next move from the engine of the specified instance(key).
- HELLO~{key}~

      Writes hello in the api console and responds with success.
- ECHO~{key}~{message to echo}

      Writes the specified message in th eapi console and responds with success.
