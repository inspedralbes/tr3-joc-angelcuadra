const matchRepository = require('../repositories/MatchRepository');
const userRepository = require('../repositories/userRepository');

module.exports = (io) => {
  io.on('connection', (socket) => {
    console.log('Nou client connectat:', socket.id);

    // Unir-se al Lobby (per rebre llista de partides)
    socket.on('joinLobby', () => {
      socket.join('lobby');
      io.to('lobby').emit('matchesUpdated', JSON.stringify(matchRepository.getWaitingMatches()));
    });

    // Crear una nova partida
    socket.on('createMatch', ({ hostId, roomName, color }) => {
      console.log(`Peticio de createMatch rebuda de ${hostId} per la sala ${roomName}`);
      const match = matchRepository.createMatch(hostId, roomName, color);
      socket.join(`match_${match.id}`);
      
      // Actualitzar lobby
      console.log("Actualitzant llista de partides al lobby...");
      io.to('lobby').emit('matchesUpdated', JSON.stringify(matchRepository.getWaitingMatches()));
      
      // Confirmar al host
      socket.emit('matchCreated', JSON.stringify(match));
      console.log(`Partida creada amb ID: ${match.id}`);
    });

    // Unir-se a una partida existent
    socket.on('joinMatch', ({ matchId, playerId, color }) => {
      console.log(`Peticio de joinMatch rebuda: matchId=${matchId}, playerId=${playerId}, color=${color}`);
      const mId = Number(matchId);
      const success = matchRepository.addPlayerToMatch(mId, playerId, color);
      console.log(`Resultat addPlayerToMatch: ${success}`);

      if (success) {
        socket.join(`match_${mId}`);
        const match = matchRepository.getMatch(mId);
        
        // Notificar a la sala
        io.to(`match_${mId}`).emit('playerJoined', JSON.stringify(match));
        
        // Si ja som 2 jugadors, iniciem
        console.log(`Jugadors a la partida ${mId}: ${match.players.length}`);
        if (match.players.length >= 2) {
          console.log(`Iniciant partida ${mId}!`);
          matchRepository.updateMatchStatus(mId, 'playing');
          io.to(`match_${mId}`).emit('matchStarted', JSON.stringify(match));
          io.to('lobby').emit('matchesUpdated', JSON.stringify(matchRepository.getWaitingMatches())); // Esborrar del lobby
        }
      } else {
        socket.emit('error', { message: 'No s\'ha pogut unir a la partida' });
      }
    });

    // Rebre moviment (Tron/Snake)
    socket.on('playerMove', ({ matchId, playerId, position, direction }) => {
      // Reenviar a la resta de jugadors de la sala (menys a l'emissor)
      socket.to(`match_${matchId}`).emit('opponentMoved', JSON.stringify({
        playerId,
        position,
        direction
      }));
    });

    // Rebre col·lisió (Fi del joc)
    socket.on('playerCollision', async ({ matchId, loserId }) => {
      const mId = Number(matchId);
      console.log(`COL·LISIÓ REBUDA: El jugador ${loserId} diu que ha mort a la partida ${mId}`);
      
      const match = matchRepository.getMatch(mId);
      if (match && match.status === 'playing') {
        console.log(`Llista de jugadors a la partida: ${JSON.stringify(match.players)}`);
        const winnerId = match.players.find(id => id !== loserId);
        console.log(`ID Perdedor: ${loserId} | ID Guanyador Detectat: ${winnerId}`);
        
        matchRepository.updateMatchStatus(mId, 'finished');
        match.winnerId = winnerId;
        
        // Actualitzar estadístiques a MongoDB (Asíncron)
        try {
          console.log(`Intentant actualitzar estadístiques per ${loserId} (Loss)...`);
          await userRepository.updateStats(loserId, false);
          if (winnerId) {
            console.log(`Intentant actualitzar estadístiques per ${winnerId} (Win)...`);
            await userRepository.updateStats(winnerId, true);
            console.log(`Estadístiques actualitzades a la BD.`);
          }
        } catch (err) {
          console.error("Error actualitzant estadístiques a MongoDB:", err);
        }

        const endData = {
          winnerId: winnerId ? winnerId.toString() : null,
          loserId: loserId ? loserId.toString() : null
        };
        console.log("Enviant matchEnded:", endData);
        io.to(`match_${mId}`).emit('matchEnded', JSON.stringify(endData));
      } else {
        console.warn(`Col·lisió ignorada: Partida ${mId} no trobada o ja finalitzada (Status: ${match ? match.status : 'N/A'})`);
      }
    });

    socket.on('disconnect', () => {
      console.log('Client desconnectat:', socket.id);
      // Faltaria gestionar si un jugador marxa enmig d'una partida
    });
  });
};
