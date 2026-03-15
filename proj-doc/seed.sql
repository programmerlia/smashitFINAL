USE dbsmashitFINAL;
GO
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    /* =========================================================
       0) Seed dates
       ========================================================= */
    DECLARE @SeedDates TABLE ([D] DATE PRIMARY KEY);
    INSERT INTO @SeedDates ([D])
    VALUES  ('2026-03-15'), ('2026-03-16'), ('2026-03-17'), ('2026-03-18');

    /* =========================================================
       1) STAFF + PLAYERS
       ========================================================= */
    INSERT INTO tblStaffAccount (Lastname, Firstname, Email, Username, [Password], RoleName)
    VALUES 
    ('Olayvar', 'Hannali', 'hannaliolayvar@gmail.com', 'admin', 'pass', 'admin'),
    ('Noda', 'Isaiah', 'isaiahandreinoda@gmail.com', 'receptionist', 'pass', 'receptionist'),
    ('Par', 'Dean', 'isaiahandreinoda@gmail.com', 'queuemaster', 'pass', 'queuemaster');


    DECLARE @AdminStaffID INT = 1

    INSERT INTO tblPlayerAccount (Lastname, Firstname, Email, PhoneNumber, Username, [Password])
    VALUES 
    ('Olayvar', 'Elyza', 'olayvarelyzarosa@gmail.com', '09171234567', 'elyza', 'pass'),
    ('Olayvar', 'Elexali', 'elexaliolayvar9711@gmail.com', '09179876543', 'elexali', 'pass');

    DECLARE @UserElyza INT = (SELECT UserID FROM tblPlayerAccount WHERE Username='elyza');
    DECLARE @UserElexali INT = (SELECT UserID FROM tblPlayerAccount WHERE Username='elexali');

    /* =========================================================
       2) COURTS
       ========================================================= */
    INSERT INTO tblCourt(CourtNumber, SportName, IsActive) 
    VALUES 
    (1, 'badminton', 1), (2, 'badminton', 1), (3, 'badminton', 1),
    (4, 'badminton', 1), (5, 'pickleball', 1), (6, 'pickleball', 1);

    DECLARE @Court1 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=1);
    DECLARE @Court2 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=2);
    DECLARE @Court3 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=3);
    DECLARE @Court5 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=5);
    DECLARE @Court6 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=6);

    /* =========================================================
       3) EQUIPMENT MODELS + ITEMS
       ========================================================= */
    -- Note: Added ItemCategory to satisfy CHECK constraint
    INSERT INTO tblEquipmentModel(EquipmentType, EquipmentSpec, DefaultRentalPrice, ItemCategory)
    VALUES 
    ('Badminton Racket', 'JXXS-10', 100.00, 'Rental'),
    ('Badminton Racket', 'JXXS-11', 100.00, 'Rental'),
    ('ShuttleCock', 'AULA', 50.00, 'Consumable'),
    ('Pickleball Paddle', 'JOOLA FXS-26', 100.00, 'Rental');

    DECLARE @M_J10 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='JXXS-10');
    DECLARE @M_J11 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='JXXS-11');
    DECLARE @M_AULA INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='AULA');
    DECLARE @M_F26 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='JOOLA FXS-26');

    INSERT INTO tblEquipmentItem(ModelID) 
    VALUES (@M_J10), (@M_J10), (@M_J11), (@M_J11), (@M_F26), (@M_F26);

    DECLARE @ItemA INT = (SELECT MIN(ItemID) FROM tblEquipmentItem);
    DECLARE @ItemB INT = (SELECT MAX(ItemID) FROM tblEquipmentItem);

 
    COMMIT TRAN;
    PRINT 'Seed successful!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000)=ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
GO


INSERT INTO tblAboutUsMembers (Firstname, Lastname, Position, ImgPath)
VALUES
('Harvey', 'Lafuente', 'Docu', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773411879/smash-it-uploads/vzmcjezpwh0jet9bl0xc.jpg'),
('Isaiah', 'Noda', 'Leader', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773411941/smash-it-uploads/szlmkxs8ubt3zarbirv8.jpg'),
('Hannali', 'Olayvar', 'Lahat', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773411970/smash-it-uploads/fng3ktsottdxltqu6knb.jpg'),
('Dean', 'Par', 'Wala', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773412001/smash-it-uploads/nhkvyv5wwajqgfeykwra.jpg'),
('Mark', 'Unira', 'Backend', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773412022/smash-it-uploads/vergazexkxyk9lkcda6b.jpg');

INSERT INTO tblAnnouncement (Title, Content, FilePath, StartDate, EndDate, CreatedAt, ViewStatus, DisplayOrder, CreatedByStaffID, URL_FB)
VALUES
('anno-hero', 'Hero Banner Image', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773410082/smash-it-uploads/xchcd0bq13eugchwj5ug.jpg', '2026-03-13 21:54:34.527', NULL, '2026-03-13 21:54:34.527', 'True', 0, 2, NULL),
('1st Anniversary Founders Cup Tournament!', 'Celebrate with us and be part of the action on April 19, 2026.
Registration is now open! Secure your slot and don’t miss the excitement.
For more details, message Coach Ja – 0923 529 6601 or send a message to our page.', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773410180/smash-it-uploads/wgl92cupp0ckdviqsloi.jpg', '2026-03-13 21:56:11.577', '2026-03-19 00:00:00.000', '2026-03-13 21:56:11.577', 'True', 1, 1, 'https://www.facebook.com/share/p/18HB8xuHtz/'),
('Summer Camp Badminton Training for Kids 2026', 'Give your kids a fun and active summer on the court! 
Our training focuses on proper fundamentals, discipline, confidence, and teamwork — all while having FUN!
 Perfect for beginners and young players who want to improve their skills and stay active during vacation.', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773410245/smash-it-uploads/nnvn03f3di3ch9dgsdq3.jpg', '2026-03-13 21:57:16.340', '2026-03-13 21:57:16.340', '2026-03-13 21:57:16.340', 'True', 2, 1, 'https://www.facebook.com/share/p/1ALYK3hTzX/');

INSERT INTO tblContent (Section, Title, Subtitle, Content, ImgPath, LastModifiedByStaffID)
VALUES
('Hero-Home', 'Badminton', 'Expert or beginner, everyone is welcome! Join us on the court for some high-energy fun.', '8:00 AM - 9:00 PM: ?330/hr', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773410440/smash-it-uploads/bbqt1j2gjxbtbwkcwv8j.png', 1),
('Hero-Home', 'Pickleball', 'Easy to learn, hard to stop! Pro or beginner, we’ve got a court waiting for you.', '8:00 AM - 8:00 PM: ?330/hr', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773410710/smash-it-uploads/flsiedlsy8zocykkkrta.png', 1),
('Hero-About-Us', NULL, NULL, NULL, 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773411201/smash-it-uploads/hmt0vehcw3bcvdnasfma.jpg', 1),
('Our-Story-About-Us', NULL, NULL, 'Born in 2020 during the height of the pandemic, Smash-It Sports Center started as a small community haven for badminton and pickleball enthusiasts looking for a safe, active outlet. As our Smash-It family quickly grew into a bustling hub for athletes of all levels, our original manual booking methods—relying on messages and physical waitlists—could no longer keep up with the demand. To ensure our players spend less time waiting and more time playing, we have upgraded to a seamless digital platform that allows you to check real-time court availability, secure reservations, and track your queue status instantly. While our technology has modernized, our core mission remains exactly the same: to provide a premier, hassle-free sports destination where the community comes together to play, compete, and smash their goals.', 'https://res.cloudinary.com/dqtpqzfg9/image/upload/v1773411384/smash-it-uploads/l4860xvolkht07mnqn1i.jpg', 1),
('Mission-Vision-About-Us', NULL, NULL, 'To provide a high-quality, accessible sports facility with a seamless digital booking experience, allowing our community of badminton and pickleball players to focus on what matters most: playing the game. ; To be the region''s premier, tech-forward sports hub, setting the standard for a modernized, customer-first recreation experience that empowers everyone to stay active.', NULL, 1);