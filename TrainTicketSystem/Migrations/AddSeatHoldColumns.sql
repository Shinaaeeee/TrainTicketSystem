-- Migration: Add seat hold tracking columns to Seat table
-- Run once against TrainTicketDB

ALTER TABLE Seat
ADD SeatHoldStatus NVARCHAR(20)  NULL,   -- NULL/'Available' = empty, 'Held' = reserved, 'Booked' = confirmed
    HoldExpiredAt  DATETIME      NULL,   -- Expiry timestamp for the hold (10 minutes from hold time)
    HeldByUserId   INT           NULL;   -- FK reference to Users.UserId (no constraint, soft reference)
