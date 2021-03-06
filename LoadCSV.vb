﻿Imports System.Data.OleDb
Imports System.IO
Imports System.IO.Compression


Public Class LoadCSV
    Implements IDisposable

    Private srGetData As StreamReader

    ''' <summary>
    ''' CSV所在的文件夹路径
    ''' </summary>
    ''' <remarks></remarks>
    Private strFolderName As String
    ''' <summary>
    ''' 文件名称
    ''' </summary>
    ''' <remarks></remarks>
    Private strFileName As String
    ''' <summary>
    ''' SQL语句
    ''' </summary>
    ''' <remarks></remarks>
    Private strSelectStatement As String
    ''' <summary>
    ''' 错误信息
    ''' </summary>
    ''' <remarks></remarks>

    Private strAllFG As String



    ''' <summary>
    ''' 定义一个新的类,并初步写出读取全部CSV内容的SQL语句
    ''' </summary>
    ''' <param name="FullFileName">CSV所在的全文件名</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal FullFileName As String)
        strFolderName = FullFileName.Substring(0, FullFileName.LastIndexOf("\"))
        strFileName = IO.Path.GetFileName(FullFileName)
        strSelectStatement = "SELECT * FROM [" & strFileName & "]"
        strAllFG = ","
    End Sub

    ''' <summary>
    ''' 定义一个新的类,并初步写出读取全部CSV内容的SQL语句
    ''' </summary>
    ''' <param name="FullFileName">CSV所在的全文件名</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal FullFileName As String, ByVal strFG As String)
        strFolderName = FullFileName.Substring(0, FullFileName.LastIndexOf("\"))
        strFileName = IO.Path.GetFileName(FullFileName)
        strSelectStatement = "SELECT * FROM [" & strFileName & "]"
        strAllFG = strFG
    End Sub

    ''' <summary>
    ''' SQL的连接语句
    ''' </summary>
    ''' <param name="FolderName">文件夹名</param>
    ''' <param name="strFG">分隔符</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function ConnectionString(ByVal FolderName As String, ByVal strFG As String) As String
        Dim Builder As New OleDbConnectionStringBuilder

        Builder.Provider = "Microsoft.ACE.OLEDB.12.0"
        Builder.Add("Extended Properties", "Text;FMT=Delimited(" & strFG & ");IMEX=1;HDR=Yes;")

        Builder.DataSource = FolderName

        Return Builder.ToString

    End Function


    ''' <summary>
    ''' 读取数据返回DataTable类型
    ''' </summary>
    ''' <returns>返回DataTable类型</returns>
    ''' <remarks></remarks>
    Public Function GetData() As DataTable
        Dim dtCSV As New DataTable

        Dim strConnection As String
        strConnection = ConnectionString(strFolderName, strAllFG)

        Try
            Using cn As New OleDbConnection With {.ConnectionString = strConnection}
                Using cmd As New OleDbCommand With {.Connection = cn, .CommandText = strSelectStatement}
                    cn.Open()
                    dtCSV.Load(cmd.ExecuteReader)
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception(ex.Message, ex)

        End Try

        Return dtCSV
    End Function

    Public Function SaveASNewOne(ByRef dtFromData As DataTable) As Boolean
        Dim i As Integer
        Dim drDataOrg As DataRow
        Dim dcDataOrg As DataColumn
        Dim strPathSaveFile As String
        Dim swSaveStream As StreamWriter
        Dim strtmpOutput As String
        Dim intMaxItemNum As Integer
        Dim j As Integer
        Try

            strPathSaveFile = strFolderName & "\" & strFileName
            If System.IO.File.Exists(strPathSaveFile) Then
                swSaveStream = New StreamWriter(File.Open(strPathSaveFile, FileMode.Open), Text.Encoding.GetEncoding("GB2312"))
            Else
                swSaveStream = New StreamWriter(File.Open(strPathSaveFile, FileMode.Create), Text.Encoding.GetEncoding("GB2312"))

            End If
            strtmpOutput = ""
            For Each dcDataOrg In dtFromData.Columns
                strtmpOutput += dcDataOrg.Caption & ","
            Next
            swSaveStream.Write(strtmpOutput & vbCrLf)

            intMaxItemNum = dtFromData.Columns.Count - 1


            j = 0

            For Each drDataOrg In dtFromData.Rows
                strtmpOutput = ""
                For i = 0 To intMaxItemNum
                    strtmpOutput += drDataOrg.Item(i) & ","
                Next
                swSaveStream.Write(strtmpOutput & vbCrLf)
                If j > 5000 Then
                    swSaveStream.Flush()

                End If
            Next
        Catch ex As Exception
            Throw New Exception(ex.Message, ex)
        Finally
            If swSaveStream IsNot Nothing Then
                swSaveStream.Flush()
                swSaveStream.Close()
                swSaveStream = Nothing
            End If
        End Try

        Return True
    End Function




    Public Sub CompressFile(ByVal sourceFile As String, ByVal destinationFile As String)
        '压缩档案
        If Not File.Exists(sourceFile) Then
            Throw New FileNotFoundException
        End If

        Dim sourceStream As FileStream = Nothing
        Dim destinationStream As FileStream = Nothing
        Dim compressedStream As GZipStream = Nothing

        Try
            'Read the bytes from the source file into a byte array
            sourceStream = New FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read)
            'Read the source stream values into the buffer
            Dim buffer(sourceStream.Length - 1) As Byte
            Dim checkCounter As Integer = sourceStream.Read(buffer, 0, buffer.Length)

            If checkCounter <> buffer.Length Then
                Throw New ApplicationException
            End If

            'Open the FileStream to write to
            destinationStream = New FileStream(destinationFile, FileMode.OpenOrCreate, FileAccess.Write)

            'Create a compression stream pointing to the destiantion stream
            compressedStream = New GZipStream(destinationStream, CompressionMode.Compress, True)

            'Now write the compressed data to the destination file
            compressedStream.Write(buffer, 0, buffer.Length)
        Catch ex As ApplicationException
            Throw New Exception(ex.Message, ex)
        Finally
            'Make sure we allways close all streams
            If sourceStream IsNot Nothing Then
                sourceStream.Close()
            End If
            If compressedStream IsNot Nothing Then
                compressedStream.Close()
            End If

            If destinationStream IsNot Nothing Then
                destinationStream.Close()
            End If
        End Try
    End Sub



    ''' <summary>
    ''' 读取数据返回DataTable类型
    ''' </summary>
    ''' <returns>返回DataTable类型</returns>
    ''' <remarks></remarks>
    Public Function GetDataViaTxtReader(ByVal dtFormat As DataTable) As DataTable
        Dim dtCSV As New DataTable
        Dim strEveryLine As String
        Dim strTmpListOfData() As String
        Dim intHowManyCol As Integer
        Dim i As Integer
        Dim j As Integer
        Dim drTmp As DataRow
        Dim intTmpListOfTitle() As Integer



        Try
            srGetData = New StreamReader(strFolderName & "\" & strFileName, System.Text.Encoding.GetEncoding("GB2312"))
            strEveryLine = srGetData.ReadLine()
            strTmpListOfData = strEveryLine.Split(strAllFG)
            ReDim intTmpListOfTitle(0 To (strTmpListOfData.Count - 1))


            '------------------------
            '引用标题名来查找


            '读取格式表的列数
            intHowManyCol = dtFormat.Columns.Count

            '创建新数据表的列标题以及数据类型
            For i = 1 To intHowManyCol
                dtCSV.Columns.Add(New System.Data.DataColumn(dtFormat.Columns(i - 1).ColumnName, dtFormat.Columns(i - 1).DataType))
            Next i

            '------------------存在无法读取的情况

            intHowManyCol = strTmpListOfData.Count
            For i = 1 To intHowManyCol
                If dtFormat.Columns(strTmpListOfData(i - 1)) IsNot Nothing Then

                    intTmpListOfTitle(i - 1) = dtFormat.Columns(strTmpListOfData(i - 1)).Ordinal
                Else
                    intTmpListOfTitle(i - 1) = -1

                End If

            Next i


            strEveryLine = srGetData.ReadLine()


            While (strEveryLine <> Nothing)
                strTmpListOfData = strEveryLine.Split(strAllFG)
                '每行数据表的来读
                drTmp = dtCSV.NewRow
                For j = 1 To dtCSV.Columns.Count
                    Try
                        If intTmpListOfTitle(j - 1) >= 0 Then

                            If strTmpListOfData(j - 1) = "#DIV/0" Or strTmpListOfData(j - 1) = "" Or strTmpListOfData(j - 1) = "#N/A" Then
                                drTmp(intTmpListOfTitle(j - 1)) = DBNull.Value

                            Else
                                drTmp(intTmpListOfTitle(j - 1)) = CTypeDynamic(strTmpListOfData(j - 1), dtFormat.Columns(j - 1).DataType)
                            End If
                        End If
                    Catch
                        drTmp(intTmpListOfTitle(j - 1)) = DBNull.Value
                    End Try
                Next j
                dtCSV.Rows.Add(drTmp)

                strEveryLine = srGetData.ReadLine()

            End While

        Catch ex As Exception
            Throw New Exception(ex.Message, ex)

        End Try

        Return dtCSV
    End Function



#Region "IDisposable Support"
    Private disposedValue As Boolean ' 检测冗余的调用

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO:  释放托管状态(托管对象)。
            End If

            ' TODO:  释放非托管资源(非托管对象)并重写下面的 Finalize()。
            ' TODO:  将大型字段设置为 null。
        End If
        Me.disposedValue = True
    End Sub

    ' TODO:  仅当上面的 Dispose(ByVal disposing As Boolean)具有释放非托管资源的代码时重写 Finalize()。
    'Protected Overrides Sub Finalize()
    '    ' 不要更改此代码。    请将清理代码放入上面的 Dispose(ByVal disposing As Boolean)中。
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' Visual Basic 添加此代码是为了正确实现可处置模式。
    Public Sub Dispose() Implements IDisposable.Dispose
        ' 不要更改此代码。    请将清理代码放入上面的 Dispose (disposing As Boolean)中。
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class
