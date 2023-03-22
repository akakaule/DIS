import * as React from "react";
import FilterContext from "./filtering-context";
import { InputLabel, Box, FormControl, MenuItem, Checkbox, OutlinedInput, ListItemText, TextField } from '@material-ui/core';
import { createStyles, makeStyles, Theme } from '@material-ui/core/styles';

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        root: {
            width: '-webkit-fill-available',
            paddingLeft: 16,
        },
        
    }),
);

type SessionFilteringProps = {

}

const SessionFiltering = (props: SessionFilteringProps) => {
    const classes = useStyles();

    const ctx = React.useContext(FilterContext);
    const [sessionId, setSessionId] = React.useState<string>();


    React.useEffect(() => {
        const filters = ctx.filterContext;
        filters.sessionId = sessionId;
        ctx.setProjectContext(filters);
    }, [sessionId]);

    return (
        <div className={classes.root}>
            <TextField
                id="outlined-basic"
                label="SessionId"
                variant="outlined"
                size="small"
                margin="none"
                style={{width: "100%"}}
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                    setSessionId(event.target.value);
                }}
            />
        </div>)
}

export default SessionFiltering;