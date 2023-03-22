import { Box } from '@chakra-ui/react';
import React, { useState } from 'react';
import './loading.css'

const Loading = (props: {diameter?: number}) => {
    const [diameter] = useState<number>(props.diameter || 100);

    return (
        <Box className="loading" width={diameter} height={diameter}>
            <Box border="2px solid" borderColor="gray.500"/>
            <Box/>
        </Box>
    );
};

export default Loading;