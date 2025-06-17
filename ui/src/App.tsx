import React, { useState } from 'react';
import { AppBar, Box, Button, Tab, Tabs, TextField, Typography, Paper, Grid, FormControlLabel, Checkbox, CircularProgress } from '@mui/material';
import { CloudUpload } from '@mui/icons-material';
import { styled } from '@mui/material/styles';
import LoadingOverlay from './components/LoadingOverlay';

function TabPanel(props: { children?: React.ReactNode; index: number; value: number }) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`tabpanel-${index}`}
      aria-labelledby={`tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const Input = styled('input')({
  display: 'none',
});

const DataFolder = process.env.REACT_APP_DATA_FOLDER || '/data';

const App: React.FC = () => {
  const [tab, setTab] = useState(0);
  const [chatText, setChatText] = useState('');
  const [chatResponse, setChatResponse] = useState('');
  const [useEmbeddings, setUseEmbeddings] = useState(false);
  const [fineTuneText, setFineTuneText] = useState('');
  const [uploadFiles, setUploadFiles] = useState<File[]>([]);
  const [uploadStatus, setUploadStatus] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTab(newValue);
  };

  const handleChatSubmit = async () => {
    setIsLoading(true);
    try {
      const response = await fetch(`${process.env.REACT_APP_API_URL}/chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userMessage: chatText,
          useEmbeddings: useEmbeddings,
        }),
      });
      if (response.ok) {
        const data = await response.json();
        setChatResponse(data.llmResponse);
      } else {
        setChatResponse('Error: Could not get response from LLM.');
      }
    } catch (error) {
      console.error('Error sending chat message:', error);
      setChatResponse('Error: Failed to connect to backend.');
    } finally {
      setIsLoading(false);
    }

    setChatText('');
  };

  const handleFineTuneSubmit = async () => {
    setIsLoading(true);
    try {
      const response = await fetch(`${process.env.REACT_APP_API_URL}/finetune`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userMessage: fineTuneText,
          useEmbeddings: false,
        }),
      });

      if (!response.ok) {
        setChatResponse('Error: Could not get response from server.');
      }

    } catch (error) {
      console.error('Error sending fine tune message:', error);
      setChatResponse('Error: Failed to connect to backend.');
    } finally {
      setIsLoading(false);
    }

    setFineTuneText('');
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setUploadFiles(Array.from(e.target.files));
    }
  };

  const handleFileDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (e.dataTransfer.files) {
      setUploadFiles(Array.from(e.dataTransfer.files));
    }
  };

  const handleUpload = async () => {
    if (uploadFiles.length === 0) return;
    setUploadStatus('Uploading...');
    setIsLoading(true);
    const formData = new FormData();
    uploadFiles.forEach(file => formData.append('files', file));
    try {
      const res = await fetch(`${process.env.REACT_APP_API_URL}/process-pdfs`, {
        method: 'POST',
        body: formData,
      });
      if (res.ok) {
        setUploadStatus('Upload successful!');
        setUploadFiles([]);
      } else {
        setUploadStatus('Upload failed.');
      }
    } catch (err) {
      setUploadStatus('Upload error.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box sx={{ width: '100%' }}>
      <AppBar position="static">
        <Tabs value={tab} onChange={handleTabChange} aria-label="main tabs">
          <Tab label="Chat" id="tab-0" aria-controls="tabpanel-0" sx={{ bgcolor: tab === 0 ? 'action.selected' : 'transparent' }} />
          <Tab label="Upload" id="tab-1" aria-controls="tabpanel-1" sx={{ bgcolor: tab === 1 ? 'action.selected' : 'transparent' }} />
          <Tab label="Fine Tune" id="tab-2" aria-controls="tabpanel-2" sx={{ bgcolor: tab === 2 ? 'action.selected' : 'transparent' }} />
        </Tabs>
      </AppBar>
      <TabPanel value={tab} index={0}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6">Chat</Typography>
          <TextField
            label="LLM Response"
            multiline
            minRows={6}
            fullWidth
            value={chatResponse}
            sx={{ mt: 2 }}
          />
          <TextField
            label="Type your message"
            multiline
            minRows={4}
            fullWidth
            value={chatText}
            onChange={e => setChatText(e.target.value)}
            sx={{ my: 2 }}
          />
          <FormControlLabel
            control={<Checkbox checked={useEmbeddings} onChange={e => setUseEmbeddings(e.target.checked)} />}
            label="Use Embeddings"
            sx={{ mb: 2 }}
          />
          <Button variant="contained" onClick={handleChatSubmit}>
            Submit
          </Button>
        </Paper>
      </TabPanel>
      <LoadingOverlay isLoading={isLoading} />
      <TabPanel value={tab} index={1}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6">Upload Files</Typography>
          <Box
            onDrop={handleFileDrop}
            onDragOver={e => e.preventDefault()}
            sx={{ border: '2px dashed #aaa', borderRadius: 2, p: 4, textAlign: 'center', mb: 2 }}
          >
            <CloudUpload sx={{ fontSize: 40, mb: 1 }} />
            <Typography>Drag and drop files here, or</Typography>
            <label htmlFor="file-upload">
              <Input
                id="file-upload"
                type="file"
                multiple
                onChange={handleFileChange}
              />
              <Button variant="outlined" component="span" sx={{ mt: 1 }}>
                Pick Files
              </Button>
            </label>
          </Box>
          {uploadFiles.length > 0 && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2">Files to upload:</Typography>
              <ul>
                {uploadFiles.map(file => (
                  <li key={file.name}>{file.name}</li>
                ))}
              </ul>
            </Box>
          )}
          <Button
            variant="contained"
            startIcon={<CloudUpload />}
            onClick={handleUpload}
            disabled={uploadFiles.length === 0}
          >
            Upload
          </Button>
          {uploadStatus && <Typography sx={{ mt: 2 }}>{uploadStatus}</Typography>}
        </Paper>
      </TabPanel>
      <TabPanel value={tab} index={2}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6">Fine Tune</Typography>
          <TextField
            label="Paste your fine-tuning data"
            multiline
            minRows={6}
            fullWidth
            value={fineTuneText}
            onChange={e => setFineTuneText(e.target.value)}
            sx={{ my: 2 }}
          />
          <Button variant="contained" onClick={handleFineTuneSubmit}>Submit</Button>
        </Paper>
      </TabPanel>
    </Box>
  );
};

export default App;
