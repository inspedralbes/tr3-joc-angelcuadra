const matchRepository = require('../repositories/MatchRepository');
const userRepository = require('../repositories/UserRepository');

module.exports = (io) => {
  io.on('connection', (socket) => {
    console.log('Nou client connectat:', socket.id);

    // Unir-se al Lobby (per rebre llista de partides)
    socket.on('joinLobby', () => {
      socket.join('lobby');
      io.to('lobby').emit('matchesUpdated', matchRepository.getWaitingMatches());
    });

    // Crear una nova partida
    socket.on('createMatch', ({ hostId, roomName }) => {
      const match = matchRepository.createMatch(hostId, roomName);
      socket.join(`match_${match.id}`);
      
      // Actualitzar lobby
      io.to('lobby').emit('matchesUpdated', matchRepository.getWaitingMatches());
      
      // Confirmar al host
      socket.emit('matchCreated', match);
    });

    // Unir-se a una partida existent
    socket.on('joinMatch', ({ matchId, playerId }) => {
      const success = matchRepository.addPlayerToMatch(matchId, playerId);
      if (success) {
        socket.join(`match_${matchId}`);
        const match = matchRepository.getMatch(matchId);
        
        // Notificar a la sala
        io.to(`match_${matchId}`).emit('playerJoined', match);
        
        // Si ja som 2 jugadors, iniciem
        if (match.players.length >= 2) {
          matchRepository.updateMatchStatus(matchId, 'playing');
          io.to(`match_${matchId}`).emit('matchStarted', match);
          io.to('lobby').emit('matchesUpdated', matchRepository.getWaitingMatches()); // Esborrar del lobby
        }
      } else {
        socket.emit('error', { message: 'No s\'ha pogut unir a la partida' });
      }
    });

    // Rebre moviment (Tron/Snake)
    socket.on('playerMove', ({ matchId, playerId, position, direction }) => {
      // Reenviar a la resta de jugadors de la sala (menys a l'emissor)
      socket.to(`match_${matchId}`).emit('opponentMoved', {
        playerId,
        position,
        direction
      });
    });

    // Rebre col·lisió (Fi del joc)
    socket.on('playerCollision', ({ matchId, loserId }) => {
      const match = matchRepository.getMatch(matchId);
      if (match && match.status === 'playing') {
        const winnerId = match.players.find(id => id !== loserId);
        
        matchRepository.updateMatchStatus(matchId, 'finished');
        match.winnerId = winnerId;
        
        // Actualitzar estadístiques
        userRepository.updateStats(loserId, false);
        if (winnerId) {
          userRepository.updateStats(winnerId, true);
        }

        io.to(`match_${matchId}`).emit('matchEnded', {
          winnerId,
          loserId
        });
      }
    });

    socket.on('disconnect', () => {
      console.log('Client desconnectat:', socket.id);
      // Faltaria gestionar si un jugador marxa enmig d'una partida
    });
  });
};
