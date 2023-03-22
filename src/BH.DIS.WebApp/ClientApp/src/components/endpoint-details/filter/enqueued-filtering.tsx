import * as React from "react";
import FilterContext from "./filtering-context";
import {TextField } from '@material-ui/core';
import { createStyles, makeStyles, Theme } from '@material-ui/core/styles';
import moment from "moment";

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        container: {
            display: 'flex',
            flexWrap: 'wrap',
        },
        textField: {
            marginLeft: theme.spacing(1),
            marginRight: theme.spacing(1),
            width: '-webkit-fill-available',
            paddingBottom: '0.5rem'
        },
    }),
);

type EnqueuedFilteringProps = {

}

const EnqueuedFiltering = (props: EnqueuedFilteringProps) => {
    const classes = useStyles();

    const ctx = React.useContext(FilterContext);
    const [enqueuedTo, setEnqueuedTo] = React.useState<string>();
    const [enqueuedFrom, setEnqueuedFrom] = React.useState<string>();


    React.useEffect(() => {
        const filters = ctx.filterContext;
        filters.enqueuedAtTo = moment(enqueuedTo, 'YYYY-MM-DD[T]HH:mm:ss');
        filters.enqueuedAtFrom = moment(enqueuedFrom, 'YYYY-MM-DD[T]HH:mm:ss');
        ctx.setProjectContext(filters);
    }, [enqueuedTo, enqueuedFrom]);

    return (
        <form className={classes.container} noValidate={true}>
            <TextField
                id="datetime-local"
                label="Updated From"
                type="datetime-local"
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                    setEnqueuedFrom(event.target.value);
                }}
                className={classes.textField}
                InputLabelProps={{
                    shrink: true,
                }}
            />
            <TextField
                id="datetime-local"
                label="Updated To"
                type="datetime-local"
                className={classes.textField}
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                    setEnqueuedTo(event.target.value);
                }}
                InputLabelProps={{
                    shrink: true,
                }}
            />
        </form>
        )
}

export default EnqueuedFiltering;