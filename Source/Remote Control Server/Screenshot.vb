﻿Imports System.Drawing
Imports System.Threading

Module Screenshot

    Public screenIndex As Integer = 0 'For multiple monitors
    Public isSendingBitmap As Boolean = False

    Public Function getScreenShot(ByVal fullscreen As Boolean, ByVal index As Integer) As Bitmap
        Dim screenGrab As Bitmap
        Try
            'Get current screen
            Dim locations As Point() = getScreenBounds(index)
            Dim startLocation As Point = locations(0)
            Dim endLocation As Point = locations(1)
            Dim screenSize As Size = New Size(endLocation.X - startLocation.X, endLocation.Y - startLocation.Y)

            screenGrab = New Bitmap(screenSize.Width, screenSize.Height)
            Dim g As Graphics = Graphics.FromImage(screenGrab)
            g.CopyFromScreen(startLocation, New Point(0, 0), screenSize)

            'Scale image down
            Dim gr_dest As Graphics
            Dim bm_dest As Bitmap
            If fullscreen Then
                bm_dest = New Bitmap(CInt(screenGrab.Width * Settings.screenScaleFull), CInt(screenGrab.Height * Settings.screenScaleFull))
            Else
                bm_dest = New Bitmap(CInt(screenGrab.Width * Settings.screenScale), CInt(screenGrab.Height * Settings.screenScale))
            End If
            gr_dest = Graphics.FromImage(bm_dest)
            gr_dest.DrawImage(screenGrab, 0, 0, bm_dest.Width + 1, bm_dest.Height + 1)
            screenGrab = bm_dest

            'Cut off black borders (Powerpoint)
            removeBlackBorders(screenGrab)

        Catch ex As Exception
            screenGrab = New Bitmap(My.Resources.ic_action_warning)
            Logger.add("Can't get screenshot:" & vbNewLine & ex.ToString)
        End Try
        Return screenGrab
    End Function

    Public Sub removeBlackBorders(ByRef bitmap As Bitmap)
        Dim x As Integer = 0
        Dim y As Integer = bitmap.Height / 2
        Dim color_left As Color
        Dim color_right As Color
        Dim isBlack As Boolean = True
        Try
            Do While isBlack
                color_left = bitmap.GetPixel(x, y)
                color_right = bitmap.GetPixel(bitmap.Width - x - 1, bitmap.Height - y - 1)

                If color_left.GetBrightness = 0 And color_right.GetBrightness = 0 Then
                    x += 5
                Else
                    isBlack = False
                End If
            Loop

            Dim CropRect As New Rectangle(x, y, bitmap.Width - x - x, bitmap.Height - y - y)
            Dim CropImage = New Bitmap(CropRect.Width, CropRect.Height)
            Using grp = Graphics.FromImage(CropImage)
                grp.DrawImage(bitmap, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
            End Using
            bitmap = CropImage
        Catch ex As Exception
            'Logger.add("Can't crop bitmap:" & vbNewLine & ex.ToString)
        End Try
    End Sub

    Public Function getScreenBounds(ByVal index As Integer) As Point()
        Dim screens As Forms.Screen() = Forms.Screen.AllScreens
        Dim startLocation As New Point(0, 0)
        Dim endLocation As New Point(0, 0)

        If index >= 0 Then
            Dim current_screen As Forms.Screen
            If (index < screens.Count) Then
                current_screen = screens.ElementAt(index)
            Else
                current_screen = screens.ElementAt(0)
                screenIndex = 0
            End If

            startLocation.X = current_screen.Bounds.X
            startLocation.Y = current_screen.Bounds.Y
            endLocation.X = current_screen.Bounds.X + current_screen.Bounds.Width
            endLocation.Y = current_screen.Bounds.Y + current_screen.Bounds.Height
        Else
            For Each tempScreen As Forms.Screen In screens
                If tempScreen.Bounds.X < startLocation.X Then
                    startLocation.X = tempScreen.Bounds.X
                End If

                If tempScreen.Bounds.Y < startLocation.Y Then
                    startLocation.Y = tempScreen.Bounds.Y
                End If

                If tempScreen.Bounds.X + tempScreen.Bounds.Width > endLocation.X Then
                    endLocation.X = tempScreen.Bounds.X + tempScreen.Bounds.Width
                End If

                If tempScreen.Bounds.Y + tempScreen.Bounds.Height > endLocation.Y Then
                    endLocation.Y = tempScreen.Bounds.Y + tempScreen.Bounds.Height
                End If
            Next
        End If
        Dim locations As Point() = New Point() {startLocation, endLocation}
        Return locations
    End Function

    Public Sub sendBitmap(ByVal bmp As Bitmap, ByVal quality As Integer)
        Try
            isSendingBitmap = True
            Dim command As New Command
            command.source = Network.getServerIp()
            command.destination = Remote.lastCommand.source
            command.priority = RemoteControlServer.Command.PRIORITY_MEDIUM
            command.data = Converter.bitmapToByte(bmp, quality)
            command.send()
        Catch ex As Exception
            Logger.add("Can't send bitmap:" & vbNewLine & ex.ToString)
        End Try
        isSendingBitmap = False
    End Sub

    Public Sub sendScreenshot(ByVal full As Boolean)
        If Not isSendingBitmap Then
            If full Then
                sendBitmap(getScreenShot(True, screenIndex), Settings.screenQualityFull)
            Else
                sendBitmap(getScreenShot(False, screenIndex), Settings.screenQuality)
            End If
        End If
    End Sub

    'Shows the next screen if multiple screens available
    Public Sub toogleScreen()
        Dim new_index As Integer = screenIndex + 1
        Dim screens As Forms.Screen() = Forms.Screen.AllScreens
        If (new_index > screens.Count - 1) Then
            'Include all screens
            new_index = -1
        End If
        screenIndex = new_index
    End Sub

End Module
