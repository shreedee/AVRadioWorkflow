import * as React from 'react';

import ensureWaitBox, { IWaitBoxState } from './reducer';
import * as _ from 'lodash';

import { Modal, Button, ProgressBar  } from 'react-bootstrap';

import { createSaverView } from '../../bootCommon/asyncLoader';
const UploadStatusView = createSaverView<boolean>();


type ConnectedProps = IWaitBoxState ;
const WaitBox: React.StatelessComponent<ConnectedProps & { dispatch }> = ({ dispatch, currentWaits }) => {
    
    return <div>
        {currentWaits && _.keys(currentWaits).map(key => {

            const wait = currentWaits[key];
            
            if (!wait)
                return '';

            return <Modal key={key} show={true} onHide={() => dispatch(ensureWaitBox().closeWait(key))} >
                <Modal.Header closeButton >
                    <Modal.Title>{wait.waitStatusAsync.error ? <span className="text-danger">Failed</span> : 'Please wait ... '}</Modal.Title>
                </Modal.Header>

                <Modal.Body>
                    <UploadStatusView
                            asyncResult={wait.waitStatusAsync}
                            prompt={wait.description}
                    >
                    </UploadStatusView>
                    {wait.waitProgress && <ProgressBar animated striped variant="info"
                        now={wait.waitProgress.percent || 0}
                        label={wait.waitProgress.status || ''}
                    />}
                </Modal.Body>

                <Modal.Footer>
                    <Button variant="warning" disabled={!wait.waitStatusAsync.error}
                        onClick={() => dispatch(ensureWaitBox().closeWait(key))}
                    >
                        Close
                    </Button>
                </Modal.Footer>

                
            </Modal>
        }
        )}
    </div>;
}

import { connect } from 'react-redux';
export default connect<ConnectedProps, { dispatch }, {}>((state) => {
    return ensureWaitBox().getCurrentState(state) || ({} as any) ;
})(WaitBox);

