const express = require('express');
const http = require('http');
const cors = require('cors');
const { Server } = require('socket.io');

const userRoutes = require('./src/routes/userRoutes');

const app = express();
const server = http.createServer(app);

// Middleware
app.use(cors());
app.use(express.json());

// Rutes API
app.use('/api/users', userRoutes);

// WebSockets Setup
const io = new Server(server, {
  cors: {
    origin: '*', // Unity client
    methods: ['GET', 'POST']
  }
});

const gameSocket = require('./src/sockets/gameSocket');
gameSocket(io);

// Start server
const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
  console.log(`Servidor escoltant al port ${PORT}`);
});
